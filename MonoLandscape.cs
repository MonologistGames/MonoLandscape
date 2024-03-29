#if TOOLS
using Godot;
using MonoLandscape.Terrain;
using MonoLandscape.Terrain.Editor;

namespace MonoLandscape;

[Tool]
public partial class MonoLandscape : EditorPlugin
{
    private Control _toolbar;
    private TerrainImporter _terrainImporter;
    private MonoTerrain _terrain;
    public override void _EnterTree()
    {
        _toolbar = new HBoxContainer();
        _toolbar.AddChild(new TerrainImporter());
        _toolbar.Visible = false;
        AddControlToContainer(CustomControlContainer.SpatialEditorMenu, _toolbar);
    }

    public override void _Ready()
    {
        _terrainImporter = _toolbar.GetChild<TerrainImporter>(0);
    }

    public override void _ExitTree()
    {
        RemoveControlFromContainer(CustomControlContainer.SpatialEditorMenu, _toolbar);
        _toolbar.Free();
    }

    public override bool _Handles(GodotObject @object)
    {
        if (@object is MonoTerrain terrain)
        {
            _terrain = terrain;
            _terrainImporter.Terrain = terrain;
            return true;
        }
        return false;
    }
    
    public override void _MakeVisible(bool visible)
    {
        _toolbar.Visible = visible;
    }
}
#endif