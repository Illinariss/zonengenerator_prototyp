using System.Collections.Generic;
using Godot;

/// <summary>
/// Main UI control that manages map creation and switching in the prototype.
/// </summary>
public partial class Game : Control
{

    /// <summary>
    /// Node that will hold the currently displayed map scene.
    /// </summary>
    [Export]
    public Node MapContainerNode { get; set; }

    private static MapRoot CreateMap(ZoneMap zoneMap)
    {
        var mapScene = GD.Load<PackedScene>("res://scenes/map_root.tscn");
        var mapRoot = mapScene.Instantiate<MapRoot>();
        mapRoot.Initialize(zoneMap);
        return mapRoot;
    }

    /// <summary>
    /// Creates a small demo map with a couple of locations.
    /// </summary>
    /// <returns>Generated <see cref="MapRoot"/> instance.</returns>
    public MapRoot CreateSmallMap()
    {
        var locations = new List<LocationInfo>
        {
            new LocationInfo("Village", DirectionHint.NorthWest),
            new LocationInfo("Cave", DirectionHint.SouthEast)
        };

        var zoneMap = new ZoneMap(15, 15, 1500, 1500, 1, locations);
        return CreateMap(zoneMap);
    }

    /// <summary>
    /// Creates a medium sized demo map.
    /// </summary>
    public MapRoot CreateMediumMap()
    {
        var locations = new List<LocationInfo>
        {
            new LocationInfo("Town", DirectionHint.NorthWest),
            new LocationInfo("Dungeon", DirectionHint.NorthEast),
            new LocationInfo("Farm", DirectionHint.SouthWest)
        };

        var zoneMap = new ZoneMap(25, 20, 30000, 20000, 2, locations);
        return CreateMap(zoneMap);
    }

    /// <summary>
    /// Creates a large demo map with several locations.
    /// </summary>
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

        var zoneMap = new ZoneMap(40, 30, 500_000, 400_000, 3, locations);
        return CreateMap(zoneMap);
    }

    /// <summary>
    /// Called when the node enters the scene tree.
    /// </summary>
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
