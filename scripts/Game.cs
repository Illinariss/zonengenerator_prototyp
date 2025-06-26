using System.Collections.Generic;
using Godot;

public partial class Game : Control
{

    [Export] public Control MapContainerNode { get; set; }

    private static MapRoot CreateMap(int width, int height, IList<LocationInfo> locations, int seed)
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
        mapRoot.Locations = locations;

        return mapRoot;
    }

    public MapRoot CreateSmallMap()
    {
        var locations = new List<LocationInfo>
        {
            new LocationInfo("Village", DirectionHint.NorthWest),
            new LocationInfo("Cave", DirectionHint.SouthEast)
        };

        return CreateMap(15, 15, locations, 1);
    }

    public MapRoot CreateMediumMap()
    {
        var locations = new List<LocationInfo>
        {
            new LocationInfo("Town", DirectionHint.NorthWest),
            new LocationInfo("Dungeon", DirectionHint.NorthEast),
            new LocationInfo("Farm", DirectionHint.SouthWest)
        };

        return CreateMap(25, 20, locations, 2);
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

        return CreateMap(40, 30, locations, 3);
    }

    public override void _Ready()
    {
        

    }

    private void DisplayMap(MapRoot map)
    {
        foreach (Node child in MapContainerNode.GetChildren())
        {
            MapContainerNode.RemoveChild(child);
            child.QueueFree();
        }

        MapContainerNode.AddChild(map);
        map.Visible = true;
        map.Scale = Vector2.One;

        // Example event subscriptions
        map.OnLocationDiscovered += info =>
            GD.Print($"Location discovered: {info.Name}");
        var cam = map.GetNodeOrNull<MapCameraController>("Camera2D");
        if (cam != null)
        {
            cam.MakeCurrent();
            var button = GetNodeOrNull<Button>("RecenterButton");
            if (button != null)
                cam.RecenterButtonPath = cam.GetPathTo(button);

            var character = map.GetNodeOrNull<CharacterNode>("CharacterNode");
            if (character != null)
                cam.CharacterPath = cam.GetPathTo(character);
        }
    }

    private void _on_small_map_pressed()
    {
        GD.Print("_on_small_map_pressed");
        var map = CreateSmallMap();
        DisplayMap(map);
    }

    private void _on_medium_map_pressed()
    {
        GD.Print("_on_medium_map_pressed");
        var map = CreateMediumMap();
        DisplayMap(map);
    }

    private void _on_large_map_pressed()
    {
        GD.Print("_on_large_map_pressed");
        var map = CreateLargeMap();
        DisplayMap(map);
    }



}
