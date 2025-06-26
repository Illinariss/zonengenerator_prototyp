using Godot;
using System;

/// <summary>
/// Container for shared enumerations used by the map prototype.
/// </summary>
public partial class Enums : Node
{
    /// <summary>
    /// Defines the traversal type of a map tile.
    /// </summary>
    public enum ZoneType
    {
        Unknown,
        Safe,
        Dangerous,
        Unpassable,
        Water
    }
}
