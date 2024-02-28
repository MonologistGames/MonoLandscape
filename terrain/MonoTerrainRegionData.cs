using Godot;

namespace MonoLandscape.Terrain.Editor;

[Tool]
[GlobalClass]
public partial class MonoTerrainRegionData : Resource
{
    [Export]public float Min { get; set; }
    [Export]public float Max { get; set; }
    [Export]public required Image HeightmapImage { get; set; }
}