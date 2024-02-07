using Godot;
using MonoLandscape.Utilities;

namespace MonoLandscape.Terrain;

[Tool]
[GlobalClass]
[Icon("res://addons/mono_landscape/icons/Terrain.svg")]
public partial class MonoTerrain: Node3D
{
    #region Rendering
    public Camera3D RenderCamera { get; set; }
    
    [ExportGroup("Rendering")]
    [Export] public MonoTerrainData TerrainData { get; set; }
    [Export] public Shader Shader { get; set; }
    [Export(PropertyHint.Layers3DRender)] public uint RenderLayers { get; set; } = 1;
    private Mesh _patchMesh;
    #endregion
    
    #region Physics
    [ExportGroup("Physics")]
    [Export] public bool UsePhysics { get; set; } = true;
    [Export(PropertyHint.Layers3DRender)] public uint PhysicsLayers { get; set; } = 1;
    #endregion
    
    public override void _Ready()
    {
        RenderCamera = Engine.IsEditorHint() ? EditorInterface.Singleton.GetEditorViewport3D().GetCamera3D() : GetViewport().GetCamera3D();

        _patchMesh = MeshGenerator.GeneratePatch(TerrainData.PatchSize, 0.5f);
    }
}