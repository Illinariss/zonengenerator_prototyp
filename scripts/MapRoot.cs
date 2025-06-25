using Godot;
using System;
using System.Collections.Generic;

public partial class MapRoot : Node2D
{
    [Export] public int width = 100;
    [Export] public int height = 60;
    [Export] public MapGenerator? Generator;

    TileMapLayer visual, logic, fog, overlay;

    public override void _Ready()
    {
        visual = GetNode<TileMapLayer>("%TileMap_Visual");
        // logic = GetNode<TileMapLayer>("%TileMap_Logic");
        // fog = GetNode<TileMapLayer>("%TileMap_Fog");
        overlay = GetNode<TileMapLayer>("%TileMap_Overlay");

        GenerateTerrain();
    }

    Vector2I lastTileUnderMouseCoordinates;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion ev)
        {
            var tile_coords = overlay.LocalToMap(overlay.ToLocal(ev.GlobalPosition));
            if (lastTileUnderMouseCoordinates != tile_coords && usedtiles.Contains(tile_coords))
            {
                overlay.SetCell(lastTileUnderMouseCoordinates, 0, new Vector2I(0, 0));
                lastTileUnderMouseCoordinates = tile_coords;
                overlay.SetCell(tile_coords, 0, new Vector2I(2, 0));
            }
            else if (lastTileUnderMouseCoordinates != tile_coords && lastTileUnderMouseCoordinates != Vector2I.Zero)
            {
                overlay.SetCell(lastTileUnderMouseCoordinates, 0, new Vector2I(0, 0));
                lastTileUnderMouseCoordinates = Vector2I.Zero;
            }
        }
    }

    List<Vector2I> usedtiles = new List<Vector2I>();

    public void GenerateTerrain()
    {
        visual.Clear();
        // logic.Clear();
        // fog.Clear();
        // overlay.Clear();

        // Generate terrain here
        IEnumerable<Vector2I> axialCoords;
        if (Generator != null)
        {
            axialCoords = Generator.GenerateShape();
        }
        else
        {
            List<Vector2I> temp = new();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    temp.Add(HexUtils.OffsetToAxial(new Vector2I(x, y)));
            axialCoords = temp;
        }

        foreach (var axial in axialCoords)
        {
            var coords = HexUtils.AxialToOffset(axial);
            visual.SetCell(coords, 0, new Vector2I(2, 2));
            overlay.SetCell(coords, 0, new Vector2I(0, 0));
            usedtiles.Add(coords);
        }
    }


}
