using System.Collections.Generic;
using Godot;

public partial class Game : Control
{
    [Export] public PanelContainer MainContent;

    private Control previousContent;

    private static MapRoot CreateMapp(int width, int height, IList<LocationInfo> locations, int seed)
    {
        var mapScene = GD.Load<PackedScene>("res://scenes/map_root.tscn");
        var mapRoot = mapScene.Instantiate<MapRoot>();

        var generator = new MapGenerator
        {
            Width = width,
            Height = height,
            Seed = seed
        };

        var mapData = generator.Generate();
        generator.PlaceLocations(mapData, locations);

        mapRoot.Generator = generator;

        return mapRoot;
    }

    public MapRoot CreateSmallMap()
    {
        var locations = new List<LocationInfo>
        {
            new LocationInfo("Village", DirectionHint.NorthWest),
            new LocationInfo("Cave", DirectionHint.SouthEast)
        };

        return CreateMapp(15, 15, locations, 1);
    }

    public MapRoot CreateMediumMap()
    {
        var locations = new List<LocationInfo>
        {
            new LocationInfo("Town", DirectionHint.NorthWest),
            new LocationInfo("Dungeon", DirectionHint.NorthEast),
            new LocationInfo("Farm", DirectionHint.SouthWest)
        };

        return CreateMapp(25, 20, locations, 2);
    }

    public MapRoot CreateLargeMap()
    {
        var locations = new List<LocationInfo>
        {
            new LocationInfo("City", DirectionHint.NorthWest),
            new LocationInfo("Fortress", DirectionHint.NorthEast),
            new LocationInfo("Mine", DirectionHint.SouthWest),
            new LocationInfo("Castle", DirectionHint.SouthEast),
            new LocationInfo("Temple", DirectionHint.NorthEast)
        };

        return CreateMapp(40, 30, locations, 3);
    }

    public override void _Ready()
    {
        // Handles only the first child
        previousContent = MainContent.GetChildCount() > 0 ? MainContent.GetChild<Control>(0) : null;

    }

    private void DisplayMap(MapRoot map)
    {
        foreach (Node child in MainContent.GetChildren())
        {
            MainContent.RemoveChild(child);
            child.QueueFree();
        }

        MainContent.AddChild(map);
    }

    private void _on_small_map_pressed()
    {
        var map = CreateSmallMap();
        DisplayMap(map);
    }

    private void _on_medium_map_pressed()
    {
        var map = CreateMediumMap();
        DisplayMap(map);
    }

    private void _on_large_map_pressed()
    {
        var map = CreateLargeMap();
        DisplayMap(map);
    }



}
