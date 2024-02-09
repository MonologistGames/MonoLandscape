using System.Collections.Generic;
using Godot;
using MonoLandscape.Utilities;

namespace MonoLandscape.Terrain;

using RS = RenderingServer;

[Tool]
[GlobalClass]
[Icon("res://addons/mono_landscape/icons/Terrain.svg")]
public partial class MonoTerrain : Node3D
{
    private readonly int[] _patchOffsetX = [1, -1, -1, 1];
    private readonly int[] _patchOffsetY = [1, 1, -1, -1];

    private bool _initialized;

    #region Rendering

    private StringName _heightmapParam = "heightmap";
    private StringName _regionSizeParam = "region_size";
    private StringName _regionPosParam = "region_pos";
    private StringName _maxHeightParam = "max_height";
    [Export] public Camera3D RenderCamera { get; set; }

    [ExportGroup("Rendering")]
    [Export]
    public MonoTerrainData TerrainData
    {
        get => _terrainData;
        set
        {
            _terrainData = value;
            if (!_initialized) return;
            ClearUp();
            Initialize();
        }
    }
    private MonoTerrainData _terrainData;
    [Export] public Shader Shader { get; set; }
    [Export(PropertyHint.Layers3DRender)] public uint RenderLayers { get; set; } = 1;

    [Export] public float Evaluation { get; set; } = 1;
    [Export] public int MaxRegionDistance { get; set; } = 1000;

    private Rid _patchMesh;
    private readonly List<int> _regionLods = [];
    private readonly List<ImageTexture> _regionHeightmaps = [];
    private readonly List<ShaderMaterial> _terrainMaterials = [];
    private readonly List<Rid> _patchInstances = [];

    #endregion

    #region Physics

    [ExportGroup("Physics")][Export] public bool UsePhysics { get; set; } = true;
    [Export(PropertyHint.Layers3DRender)] public uint PhysicsLayers { get; set; } = 1;

    #endregion

    public override void _EnterTree()
    {
        VisibilityChanged += () =>
        {
            foreach (var patch in _patchInstances)
            {
                RS.Singleton.InstanceSetVisible(patch, Visible);
            }
        };

        GD.Print("MonoTerrain _EnterTree");
        Initialize();
        _initialized = true;
    }

    public override void _Ready()
    {
        RenderCamera = Engine.IsEditorHint()
            ? EditorInterface.Singleton.GetEditorViewport3D().GetCamera3D()
            : GetViewport().GetCamera3D();

        if (TerrainData == null) return;
        if (!Visible) return;
        var patchCount = 0;
        BuildRegions(ref patchCount);
    }

    public override void _Process(double delta)
    {
        if (TerrainData == null) return;
        if (!Visible) return;

        var patchCount = 0;
        BuildRegions(ref patchCount);

        for (int i = patchCount; i < _patchInstances.Count; i++)
        {
            RS.Singleton.InstanceSetVisible(_patchInstances[i], false);
        }
    }

    public override void _ExitTree()
    {
        _initialized = false;
        ClearUp();
    }

    private void Initialize()
    {
        if (TerrainData == null) return;
        _patchMesh = MeshGenerator.GeneratePatch(TerrainData.PatchSize + 1, 0.5f);
        var mat = new ShaderMaterial() { Shader = Shader };
        mat.SetShaderParameter(_regionSizeParam, TerrainData.PatchSize * (1 << TerrainData.Lods - 1) * 0.5f);
        mat.SetShaderParameter(_maxHeightParam, TerrainData.MaxHeight);
        for (int i = 0; i < TerrainData.Regions.Count; i++)
        {
            _regionLods.Add(-1);
            _regionHeightmaps.Add(null);
            _terrainMaterials.Add(mat.Duplicate() as ShaderMaterial);
            _terrainMaterials[i].SetShaderParameter(_regionPosParam, TerrainData.Regions[i]);
        }
    }

    private void ClearUp()
    {
        foreach (var patch in _patchInstances)
        {
            RS.FreeRid(patch);
        }
        _patchInstances.Clear();
        _regionLods.Clear();
        _terrainMaterials.Clear();
        _regionHeightmaps.Clear();
    }

    private void BuildRegions(ref int patchCount)
    {
        var scenario = GetWorld3D().Scenario;
        var regionSize = TerrainData.PatchSize * (1 << (TerrainData.Lods - 1)) * 0.5f;
        for (int i = 0; i < TerrainData.Regions.Count; i++)
        {
            var region = TerrainData.Regions[i];
            var pos = new Vector3((region.X + 0.5f) * regionSize, 0, (region.Y + 0.5f) * regionSize);
            if (pos.DistanceTo(RenderCamera.Position) > MaxRegionDistance)
            {
                _regionLods[i] = -1;
                _regionHeightmaps[i] = null;
                continue;
            }

            var maxDepth = 0;
            BuildQuadtree(i, 0, pos, RenderCamera.Position, scenario, ref patchCount, ref maxDepth);
            maxDepth = TerrainData.Lods - maxDepth - 1;

            if (maxDepth == _regionLods[i]) continue;
            _regionLods[i] = maxDepth;

            // TODO: Use StringBuilder to build the path in order to reduce GC
            _regionHeightmaps[i] = ImageTexture.CreateFromImage(GD.Load<Image>(
                $"{TerrainData.MapDirectory}/lod_{maxDepth}/tile_{region.X}_{region.Y}.res"));
            _terrainMaterials[i].SetShaderParameter(_heightmapParam, _regionHeightmaps[i]);
        }
    }

    private void BuildQuadtree(in int region, in int depth, in Vector3 position, in Vector3 viewPoint, in Rid scenario,
        ref int patchCount, ref int maxDepth)
    {
        var patchScale = 1 << (TerrainData.Lods - depth - 1);
        var patchSize = 16 * patchScale * 0.5f;
        // TODO: Add delta movement and margin to avoid flickering and frequent map loading
        var viewPos = (viewPoint / patchSize).Floor() * patchSize;
        viewPos.X += patchSize * 0.5f;
        viewPos.Z += patchSize * 0.5f;
        var distance = viewPos.DistanceTo(position);

        // TODO: Find a better way of evaluation to avoid cross LOD level patches
        if (distance / patchSize > Evaluation || depth >= TerrainData.Lods - 1)
        {
            patchCount++;
            Rid patch;
            if (patchCount <= _patchInstances.Count)
            {
                patch = _patchInstances[patchCount - 1];
                RS.Singleton.InstanceGeometrySetCastShadowsSetting(patch, RenderingServer.ShadowCastingSetting.On);
                RS.Singleton.InstanceSetVisible(patch, true);
            }
            else
            {
                patch = RS.Singleton.InstanceCreate2(_patchMesh, scenario);
                RS.Singleton.InstanceSetLayerMask(patch, RenderLayers);
                _patchInstances.Add(patch);
            }

            var transform = Transform3D.Identity.Scaled(new Vector3(patchScale, 1, patchScale));
            transform.Origin = position - new Vector3(patchSize * 0.5f, 0, patchSize * 0.5f);
            RS.Singleton.InstanceSetTransform(patch, transform);
            RS.Singleton.InstanceGeometrySetMaterialOverride(patch, _terrainMaterials[region].GetRid());
            RS.Singleton.InstanceGeometrySetShaderParameter(patch, "lod", TerrainData.Lods - depth);
            if (maxDepth < depth)
                maxDepth = depth;
            return;
        }

        patchSize /= 4;
        for (int i = 0; i < 4; i++)
        {
            var pos = new Vector3(position.X + _patchOffsetX[i] * patchSize, 0,
                position.Z + _patchOffsetY[i] * patchSize);
            BuildQuadtree(region, depth + 1, pos, viewPoint, scenario, ref patchCount, ref maxDepth);
        }
    }

    // TODO: Update Aabb of each patch to enable correct culling
    private void UpdateAabb()
    {

    }
}