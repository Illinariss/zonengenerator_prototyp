// Scripts/Map/MapGenerator.cs
using Godot;
using System;
using System.Collections.Generic;


public partial class MapGenerator : Node
{
    public int Width = 20;
    public int Height = 20;
    public int Seed = 12345;

    /// <summary>
    /// Creates an irregular collection of axial coordinates representing
    /// the playable map area. The same seed always returns the same shape.
    /// </summary>
    public List<Vector2I> GenerateShape()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Seed;

        List<Vector2I> shape = new List<Vector2I>();

        Vector2I centerOffset = new Vector2I(Width / 2, Height / 2);
        Vector2I centerAxial = HexUtils.OffsetToAxial(centerOffset);
        float baseRadius = Math.Min(Width, Height) / 2f;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Vector2I offset = new Vector2I(x, y);
                Vector2I axial = HexUtils.OffsetToAxial(offset);

                float radiusVariation = rng.RandfRange(-2f, 2f);
                if (HexUtils.Distance(centerAxial, axial) <= baseRadius + radiusVariation)
                {
                    shape.Add(axial);
                }
            }
        }

        return shape;
    }

    public Dictionary<Vector2I, Enums.ZoneType> Generate()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Seed;

        Dictionary<Vector2I, Enums.ZoneType> mapData = new Dictionary<Vector2I, Enums.ZoneType>();

        var shape = GenerateShape();

        foreach (var axial in shape)
        {
            float roll = rng.Randf();
            Enums.ZoneType type = roll switch
            {
                < 0.7f => Enums.ZoneType.Safe,
                < 0.9f => Enums.ZoneType.Dangerous,
                _ => Enums.ZoneType.Unpassable,
            };

            mapData[axial] = type;
        }

        // Beispiel: Übergang nach Norden erzwingen
        var shapeSet = new HashSet<Vector2I>(shape);
        for (int i = 0; i < Width; i++)
        {
            var pos = HexUtils.OffsetToAxial(new Vector2I(i, 0));
            if (shapeSet.Contains(pos))
                mapData[pos] = Enums.ZoneType.Safe; // oder ein spezieller Übergangstyp
        }

        return mapData;
    }
}
