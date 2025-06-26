using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Helper methods for working with axial and offset hex coordinates.
/// </summary>
public static class HexUtils
{
    private static readonly Vector2I[] axialDirections = new Vector2I[]
    {
        new Vector2I(1, 0), new Vector2I(1, -1), new Vector2I(0, -1),
        new Vector2I(-1, 0), new Vector2I(-1, 1), new Vector2I(0, 1)
    };

    /// <summary>
    /// Converts an offset coordinate to axial coordinate space.
    /// </summary>
    /// <param name="offset">Offset coordinates (odd-r layout).</param>
    /// <returns>Axial coordinates.</returns>
    public static Vector2I OffsetToAxial(Vector2I offset)
    {
        int q = offset.X - (offset.Y - (offset.Y & 1)) / 2;
        int r = offset.Y;
        return new Vector2I(q, r);
    }

    /// <summary>
    /// Converts axial coordinates to offset space (odd-r layout).
    /// </summary>
    /// <param name="axial">Axial coordinates.</param>
    /// <returns>Offset coordinates.</returns>
    public static Vector2I AxialToOffset(Vector2I axial)
    {
        int col = axial.X + (axial.Y - (axial.Y & 1)) / 2;
        int row = axial.Y;
        return new Vector2I(col, row);
    }

    /// <summary>
    /// Calculates the hex distance between two axial coordinates.
    /// </summary>
    /// <param name="a">First axial coordinate.</param>
    /// <param name="b">Second axial coordinate.</param>
    /// <returns>Distance in hexes.</returns>
    public static int Distance(Vector2I a, Vector2I b)
    {
        return (Math.Abs(a.X - b.X) + Math.Abs(a.X + a.Y - b.X - b.Y) + Math.Abs(a.Y - b.Y)) / 2;
    }

    /// <summary>
    /// Returns the six neighboring axial coordinates of the given hex.
    /// </summary>
    /// <param name="hex">Center hex.</param>
    /// <returns>List of neighboring hex coordinates.</returns>
    public static List<Vector2I> GetNeighbors(Vector2I hex)
    {
        List<Vector2I> neighbors = new List<Vector2I>();
        foreach (var dir in axialDirections)
            neighbors.Add(hex + dir);
        return neighbors;
    }

    /// <summary>
    /// Gets all axial coordinates within the specified range of a center hex.
    /// </summary>
    /// <param name="center">Center axial coordinate.</param>
    /// <param name="range">Radius measured in hexes.</param>
    /// <returns>List of axial coordinates in range.</returns>
    public static List<Vector2I> GetHexesInRange(Vector2I center, int range)
    {
        List<Vector2I> results = new List<Vector2I>();
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = Math.Max(-range, -dx - range); dy <= Math.Min(range, -dx + range); dy++)
            {
                int dz = -dx - dy;
                results.Add(center + new Vector2I(dx, dy));
            }
        }
        return results;
    }
}