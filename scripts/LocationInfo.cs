using Godot;
using System;

/// <summary>
/// Hints at which quadrant of the map a location should appear in.
/// </summary>
public enum DirectionHint
{
    NorthWest,
    NorthEast,
    SouthWest,
    SouthEast
}

/// <summary>
/// Describes a special location placed on the map.
/// </summary>
public class LocationInfo
{
    /// <summary>Name shown when the location is discovered.</summary>
    public string Name { get; set; }

    /// <summary>Quadrant hint used during placement.</summary>
    public DirectionHint Hint { get; set; }

    /// <summary>Final axial coordinates after placement.</summary>
    public Vector2I Coordinates { get; set; }

    /// <summary>
    /// Creates a new location description.
    /// </summary>
    /// <param name="name">Name of the location.</param>
    /// <param name="hint">Placement hint.</param>
    public LocationInfo(string name, DirectionHint hint)
    {
        Name = name;
        Hint = hint;
        Coordinates = Vector2I.Zero;
    }
}
