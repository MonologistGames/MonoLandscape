#if TOOLS
using Godot;

namespace MonoLandscape.Terrain.Editor;

[Tool]
public partial class TerrainImporter : Button
{
    public MonoTerrain? Terrain { get; set; }

    private ConfirmationDialog _importWindow;
    private SpinBox _patchSize;
    private SpinBox _lods;
    private Label _regionInfo;
    private LineEdit _terrainDataPath;
    private LineEdit _heightmapPath;
    private FileDialog _dirDialog;
    private FileDialog _fileDialog;

    public TerrainImporter()
    {
        Text = "Import";
        Icon = GD.Load<Texture2D>("res://addons/mono_landscape/icons/Terrain.svg");
        Pressed += () => _importWindow?.Popup();

        _importWindow = GD.Load<PackedScene>("res://addons/mono_landscape/editor/terrain/terrain_importer.tscn")
            .Instantiate<ConfirmationDialog>();
        _importWindow.Visible = false;
        _importWindow.Confirmed += Import;

        _patchSize = _importWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/PatchSizeField");
        _patchSize.ValueChanged += OnParamsChanged;
        _lods = _importWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/LodsField");
        _lods.ValueChanged += OnParamsChanged;
        _regionInfo = _importWindow.GetNode<Label>("MarginContainer/VBoxContainer/RegionInfo");

        _dirDialog = _importWindow.GetNode<FileDialog>("DirDialog");
        _dirDialog.DirSelected += (path) => _terrainDataPath!.Text = path;
        _fileDialog = _importWindow.GetNode<FileDialog>("FileDialog");
        _fileDialog.FileSelected += (path) => _heightmapPath!.Text = path;
        _terrainDataPath = _importWindow.GetNode<LineEdit>("MarginContainer/VBoxContainer/Dir/LineEdit");
        _heightmapPath = _importWindow.GetNode<LineEdit>("MarginContainer/VBoxContainer/Image/LineEdit");
        _importWindow.GetNode<Button>("MarginContainer/VBoxContainer/Dir/Button").Pressed += () => _dirDialog.Popup();
        _importWindow.GetNode<Button>("MarginContainer/VBoxContainer/Image/Button").Pressed +=
            () => _fileDialog.Popup();

        AddChild(_importWindow);
    }

    private void OnParamsChanged(double value)
    {
        _regionInfo.Text = $"Current Region Size: {_patchSize.Value * (1 << (int)_lods.Value - 1)}";
    }

    private void Import()
    {
        if (_terrainDataPath.Text is null || _heightmapPath.Text is null)
        {
            GD.PushError("Please select a directory and a heightmap image");
            return;
        }

        var terrainData = new MonoTerrainData
        {
            PatchSize = (int)_patchSize.Value,
            Lods = (int)_lods.Value,
            MapDirectory = _terrainDataPath.Text ?? string.Empty,
        };
        var heightmap = GD.Load<Image>(_heightmapPath.Text);
        var regionSize = (int)_patchSize.Value * (1 << (int)_lods.Value - 1);
        var tilesX = (heightmap.GetSize().X - 1) / regionSize;
        var tilesY = (heightmap.GetSize().Y - 1) / regionSize;
        for (var x = 0; x < tilesX; x++)
        {
            for (var y = 0; y < tilesY; y++)
            {
                terrainData.Regions.Add(new Vector2I(x, y));
                terrainData.HeightRanges.Add(new Vector2(float.MaxValue, float.MinValue));
            }
        }

        for (int i = 0; i < _lods.Value; i++)
        {
            var dir = DirAccess.Open(_terrainDataPath.Text);
            dir.MakeDir($"lod_{i}");
            for (var x = 0; x < tilesX; x++)
            {
                for (var y = 0; y < tilesY; y++)
                {
                    var tile = CreateTile(i, x, y, regionSize, heightmap, out var range);
                    var r = terrainData.HeightRanges[x * tilesX + y];
                    r.X = Mathf.Min(r.X, range.X);
                    r.Y = Mathf.Max(r.Y, range.Y);
                    terrainData.HeightRanges[x * tilesX + y] = r;
                    ResourceSaver.Singleton.Save(tile, $"{_terrainDataPath.Text}/lod_{i}/tile_{x}_{y}.res");
                }
            }
        }

        ResourceSaver.Singleton.Save(terrainData, _terrainDataPath.Text + "/terrain_data.tres");

        GD.Print("Finished Importing Terrain Data and Tiles!");
    }

    private Image CreateTile(in int lod, in int x, in int y, in int regionSize, in Image heightmap, out Vector2 range)
    {
        var max = float.MinValue;
        var min = float.MaxValue;
        var pixelGap = 1 << lod;
        var sizeCurrentLod = regionSize >> lod;
        var img = Image.Create(sizeCurrentLod + 1, sizeCurrentLod + 1, false, Image.Format.Rf);
        for (int i = 0; i < sizeCurrentLod + 1; i++)
        {
            for (int j = 0; j < sizeCurrentLod + 1; j++)
            {
                var color = heightmap.GetPixel(x * regionSize + i * pixelGap, y * regionSize + j * pixelGap);
                img.SetPixel(i, j, color);
                max = float.Max(color.R, max);
                min = float.Min(color.R, min);
            }
        }

        range = new Vector2(min, max);
        return img;
    }
}
#endif