// Scripts/Map/MapController.cs
using Godot;
using System.Collections.Generic;

public partial class MapController : Node
{
    [Export] public TileMap TileMapVisual;
    [Export] public TileMap TileMapLogic;

    [Export] public TileSet TileSetVisual;
    
    public Dictionary<Enums.ZoneType, int> TileIds = new();

    [Export] public MapGenerator Generator;

    public void _Ready()
    {
        var mapData = Generator.Generate();

        foreach (var kvp in mapData)
        {
            var offset = HexUtils.AxialToOffset(kvp.Key);
            TileMapVisual.SetCell(0, offset, TileIds[kvp.Value]);
            TileMapLogic.SetCell(0, offset, (int)kvp.Value);
        }
    }
} 