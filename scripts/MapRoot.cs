using Godot;
using System;
using System.Collections.Generic;

public partial class MapRoot : Node2D
{
    [Export] public int width = 100;
    [Export] public int height = 60;

    TileMapLayer viusal, logic, fog, overlay;

    public override void _Ready()
    {
        viusal = GetNode<TileMapLayer>("%TileMap_Visual");
        // logic = GetNode<TileMapLayer>("%TileMap_Logic");
        // fog = GetNode<TileMapLayer>("%TileMap_Fog");
        overlay = GetNode<TileMapLayer>("%TileMap_Overlay");

        GenerateTerrain();
    }

    Vector2I lasttyleundermousecoordinates;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion ev)
        {
            var tile_coords = overlay.LocalToMap(overlay.ToLocal(ev.GlobalPosition));
            if (lasttyleundermousecoordinates != tile_coords && usedtiles.Contains(tile_coords))
            {
                overlay.SetCell(lasttyleundermousecoordinates, 0, new Vector2I(0, 0));
                lasttyleundermousecoordinates = tile_coords;
                overlay.SetCell(tile_coords, 0, new Vector2I(2, 0));
            }
            else if (lasttyleundermousecoordinates != tile_coords && lasttyleundermousecoordinates != Vector2I.Zero)
            {
                overlay.SetCell(lasttyleundermousecoordinates, 0, new Vector2I(0, 0));
                lasttyleundermousecoordinates = Vector2I.Zero;
            }
        }
    }

    List<Vector2I> usedtiles = new List<Vector2I>();

    public void GenerateTerrain()
    {
        viusal.Clear();
        // logic.Clear();
        // fog.Clear();
        // overlay.Clear();

        // Generate terrain here
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var coords = new Vector2I(x, y);
                viusal.SetCell(coords, 0, new Vector2I(2, 2));
                overlay.SetCell(coords, 0, new Vector2I(0, 0));
                usedtiles.Add(coords);
            }
        }
    }


}
