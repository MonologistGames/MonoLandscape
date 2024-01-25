using Godot;

namespace TerrainSharp.Terrain;

[Tool]
[GlobalClass]
[Icon("res://addons/mono_landscape/icons/Terrain.svg")]
public partial class MonoTerrain: Node3D
{
    public Camera3D RenderCamera { get; set; }
    
    [Export] public int PatchSize { get; set; } = 127;
    [Export] public int Lods { get; set; } = 6;
    [Export]
    public Shader Shader { get; set; }
    
    public override void _Ready()
    {
        #if TOOl
        RenderCamera = GetViewport().GetCamera3D();
        #else
        #endif
    }
}