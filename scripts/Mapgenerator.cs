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

    /// <summary>
    /// Places a list of locations on passable tiles within their hinted quadrant.
    /// The method is deterministic with regard to the generator seed.
    /// </summary>
    public void PlaceLocations(Dictionary<Vector2I, Enums.ZoneType> mapData, IList<LocationInfo> locations)
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Seed;

        foreach (var location in locations)
        {
            List<Vector2I> candidates = new List<Vector2I>();

            foreach (var kvp in mapData)
            {
                if (kvp.Value == Enums.ZoneType.Unpassable)
                    continue;

                Vector2I offset = HexUtils.AxialToOffset(kvp.Key);

                bool inQuadrant = location.Hint switch
                {
                    DirectionHint.NorthWest => offset.X < Width / 2 && offset.Y < Height / 2,
                    DirectionHint.NorthEast => offset.X >= Width / 2 && offset.Y < Height / 2,
                    DirectionHint.SouthWest => offset.X < Width / 2 && offset.Y >= Height / 2,
                    DirectionHint.SouthEast => offset.X >= Width / 2 && offset.Y >= Height / 2,
                    _ => false
                };

                if (inQuadrant)
                    candidates.Add(kvp.Key);
            }

            if (candidates.Count == 0)
                continue;

            int index = rng.RandiRange(0, candidates.Count - 1);
            Vector2I chosenAxial = candidates[index];
            location.Coordinates = chosenAxial;
        }
    }
}
