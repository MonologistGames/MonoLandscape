using System;
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
    private class RegionMaterial(ShaderMaterial material)
    {
        public readonly ShaderMaterial Material = material;
        public readonly ImageTexture Heightmap = new();

        public Image? HeightmapData
        {
            get => _heightmapData;
            set
            {
                _heightmapData = value; 
                Heightmap.SetImage(_heightmapData);
            }
        }

        private Image? _heightmapData;
        public int Lod = int.MaxValue;
    }

    private readonly int[] _patchOffsetX = [1, -1, -1, 1];
    private readonly int[] _patchOffsetY = [1, 1, -1, -1];

    private bool _initialized;

    #region Rendering

    private World3D? _world;
    private StringName _heightmapParam = "heightmap";
    private StringName _regionSizeParam = "region_size";
    private StringName _regionPosParam = "region_pos";
    private StringName _maxHeightParam = "max_height";

    private ObjectPool<RegionMaterial>? _regionMaterialPool;
    // TODO: Use dictionary to replace this array
    private RegionMaterial?[] _regionContainers = null!;
    
    private Rid _patchMesh;
    private Aabb _patchAabb;
    private readonly List<Rid> _patchInstances = [];
    [Export] public Camera3D? RenderCamera { get; set; }

    [Export]
    public MonoTerrainData? TerrainData
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

    private MonoTerrainData? _terrainData;

    [ExportGroup("Rendering")]
    [Export(PropertyHint.Range, "0,10,,")]
    public int RegionBound { get; set; } = 3;

    [Export]
    public Shader TerrainShader { get; set; } =
        GD.Load<Shader>("res://addons/mono_landscape/shader/terrain/terrain.gdshader");

    [Export(PropertyHint.Layers3DRender)] public uint RenderLayers { get; set; } = 1;

    [Export] public float Evaluation { get; set; } = 1;

    #endregion

    #region Physics

    [ExportGroup("Physics")] [Export] public bool UsePhysics { get; set; } = true;
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
        _world = GetWorld3D();
    }

    public override void _Process(double delta)
    {
        if (TerrainData == null) return;
        if (!Visible) return;

        BuildRegions(out var patchCount);

        for (var i = patchCount; i < _patchInstances.Count; i++)
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
        _patchAabb = RS.Singleton.MeshGetCustomAabb(_patchMesh);
        var mat = new ShaderMaterial { Shader = TerrainShader };
        mat.SetShaderParameter(_regionSizeParam, TerrainData.PatchSize * (1 << TerrainData.Lods - 1) * 0.5f);
        mat.SetShaderParameter(_maxHeightParam, TerrainData.HeightScale);

        var visibleRange = RegionBound * 2 + 1;
        var visibleRegionCount = int.Min(visibleRange * visibleRange, TerrainData.Regions.Count);
        _regionContainers = new RegionMaterial[TerrainData.Regions.Count];
        _regionMaterialPool = new ObjectPool<RegionMaterial>(visibleRegionCount, () =>
            {
                var regionData =
                    new RegionMaterial(mat.Duplicate() as ShaderMaterial ?? throw new InvalidOperationException());
                regionData.Material.SetShaderParameter(_heightmapParam, regionData.Heightmap);
                return regionData;
            },
            regionData => { regionData.Lod = int.MaxValue; });
    }

    private void ClearUp()
    {
        foreach (var patch in _patchInstances)
        {
            RS.FreeRid(patch);
        }

        _patchInstances.Clear();
    }

    private void BuildRegions(out int patchCount)
    {
        patchCount = 0;
        if (TerrainData == null || RenderCamera == null || _world == null) return;

        var regionSize = TerrainData.PatchSize * (1 << (TerrainData.Lods - 1)) * 0.5f;
        var snappedViewPos = (RenderCamera.Position / regionSize).Floor();
        for (int i = 0; i < TerrainData.Regions.Count; i++)
        {
            var region = TerrainData.Regions[i];
            if (Mathf.Floor(snappedViewPos.X - region.X) > RegionBound ||
                Mathf.Floor(snappedViewPos.Z - region.Y) > RegionBound)
            {
                if (_regionContainers[i] is not null)
                    _regionMaterialPool?.Return(_regionContainers[i]!);
                continue;
            }

            _regionContainers[i] ??= _regionMaterialPool!.Get();
            var regionMaterial = _regionContainers[i]!;
            var pos = new Vector3((region.X + 0.5f) * regionSize, 0, (region.Y + 0.5f) * regionSize);

            var maxDepth = TerrainData.Lods - 1;
            regionMaterial.Material.SetShaderParameter(_regionPosParam, region);
            BuildQuadtree(regionMaterial, TerrainData.HeightRanges[i], maxDepth, pos,
                RenderCamera.Position,
                _world.Scenario, ref patchCount, ref maxDepth);

            if (maxDepth >= regionMaterial.Lod) continue;
            regionMaterial.Lod = maxDepth;
            // TODO: Use StringBuilder to avoid string gc
            regionMaterial.HeightmapData =
                GD.Load<Image>($"{TerrainData.MapDirectory}/lod_{maxDepth}/tile_{region.X}_{region.Y}.res");
        }
    }

    private void BuildQuadtree(in RegionMaterial region, in Vector2 range, int depth, in Vector3 position,
        in Vector3 viewPoint, in Rid scenario,
        ref int patchCount, ref int maxDepth)
    {
        if (TerrainData is null) return;

        var patchScale = 1 << depth;
        var patchSize = 16 * patchScale * 0.5f;
        // TODO: Add delta movement and margin to avoid flickering and frequent map loading
        var viewPos = (viewPoint / patchSize).Floor() * patchSize;
        viewPos.X += patchSize * 0.5f;
        viewPos.Z += patchSize * 0.5f;
        var distance = viewPos.DistanceTo(position);

        // TODO: Find a better way of evaluation to avoid cross LOD level patches
        if (distance / patchSize > Evaluation || depth <= 0)
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
            
            var aabb = _patchAabb;
            aabb.Position = new Vector3(-patchSize * 0.5f,range.X * TerrainData.HeightScale, -patchSize * 0.5f);
            aabb.End = new Vector3(patchSize * 0.5f,range.Y * TerrainData.HeightScale, patchSize * 0.5f);
            RS.Singleton.InstanceSetCustomAabb(patch, aabb);
            var transform = Transform3D.Identity.Scaled(new Vector3(patchScale, 1, patchScale));
            transform.Origin = position;
            RS.Singleton.InstanceSetTransform(patch, transform);
            RS.Singleton.InstanceGeometrySetMaterialOverride(patch, region.Material.GetRid());
            RS.Singleton.InstanceGeometrySetShaderParameter(patch, "lod", depth);
            
            if (maxDepth > depth)
                maxDepth = depth;
            return;
        }

        patchSize /= 4;
        for (int i = 0; i < 4; i++)
        {
            var pos = new Vector3(position.X + _patchOffsetX[i] * patchSize, 0,
                position.Z + _patchOffsetY[i] * patchSize);
            BuildQuadtree(region, range, depth - 1, pos, viewPoint, scenario, ref patchCount, ref maxDepth);
        }
    }
    
}