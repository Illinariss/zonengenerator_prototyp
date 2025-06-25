using Godot;
using System;

public enum DirectionHint
{
    NorthWest,
    NorthEast,
    SouthWest,
    SouthEast
}

public class LocationInfo
{
    public string Name { get; set; }
    public DirectionHint Hint { get; set; }
    public Vector2I Coordinates { get; set; }

    public LocationInfo(string name, DirectionHint hint)
    {
        Name = name;
        Hint = hint;
        Coordinates = Vector2I.Zero;
    }
}
