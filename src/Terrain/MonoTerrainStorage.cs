using Godot;
using Godot.Collections;

namespace MonoLandscape.Terrain;

[Tool]
[GlobalClass]
[Icon("res://addons/mono_landscape/icons/TerrainData.svg")]
public partial class MonoTerrainStorage : Resource
{
    [Export(PropertyHint.Dir)] public string MapDirectory { get; set; }
    [Export] public int PatchSize { get; set; } = 16;
    [Export] public int Lods { get; set; } = 6;
    [Export] public float HeightScale { get; set; } = 1 << 8;

    [Export] public Array<Vector2I> Regions { get; set; } = [];
    [Export] public Array<Vector2> HeightRanges { get; set; } = [];
}