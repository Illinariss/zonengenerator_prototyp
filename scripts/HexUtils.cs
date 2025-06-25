using Godot;
using System;
using System.Collections.Generic;

public static class HexUtils
{
    private static readonly Vector2I[] axialDirections = new Vector2I[]
    {
        new Vector2I(1, 0), new Vector2I(1, -1), new Vector2I(0, -1),
        new Vector2I(-1, 0), new Vector2I(-1, 1), new Vector2I(0, 1)
    };

    public static Vector2I OffsetToAxial(Vector2I offset)
    {
        int q = offset.X - (offset.Y - (offset.Y & 1)) / 2;
        int r = offset.Y;
        return new Vector2I(q, r);
    }

    public static Vector2I AxialToOffset(Vector2I axial)
    {
        int col = axial.X + (axial.Y - (axial.Y & 1)) / 2;
        int row = axial.Y;
        return new Vector2I(col, row);
    }

    public static int Distance(Vector2I a, Vector2I b)
    {
        return (Math.Abs(a.X - b.X) + Math.Abs(a.X + a.Y - b.X - b.Y) + Math.Abs(a.Y - b.Y)) / 2;
    }

    public static List<Vector2I> GetNeighbors(Vector2I hex)
    {
        List<Vector2I> neighbors = new List<Vector2I>();
        foreach (var dir in axialDirections)
            neighbors.Add(hex + dir);
        return neighbors;
    }

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