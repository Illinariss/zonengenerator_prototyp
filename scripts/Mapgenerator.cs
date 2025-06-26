// Scripts/Map/MapGenerator.cs
using Godot;
using System;
using System.Collections.Generic;


/// <summary>
/// Provides procedural generation of hex maps including transitions and
/// location placement.
/// </summary>
public partial class MapGenerator : Node
{
    /// <summary>Width of the map in tiles.</summary>
    public int HexagonsWidth = 20;

    /// <summary>Height of the map in tiles.</summary>
    public int HexagonsHeight = 20;

    /// <summary>Seed value used for deterministic generation.</summary>
    public int Seed = 12345;

    /// <summary>
    /// Offset coordinates that mark transitions to other maps.
    /// The value is an identifier for the destination map.
    /// </summary>
    /// <summary>
    /// Offset coordinates that mark transitions to other maps. The value is an
    /// identifier for the destination map.
    /// </summary>
    public Dictionary<Vector2I, string> TransitionTiles { get; private set; } = new();

    /// <summary>
    /// Creates an irregular collection of axial coordinates representing the
    /// playable area. The same seed always produces the same shape.
    /// </summary>
    /// <returns>List of axial coordinates describing the map shape.</returns>
    public List<Vector2I> GenerateShape()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Seed;

        List<Vector2I> shape = new List<Vector2I>();

        Vector2I centerOffset = new Vector2I(HexagonsWidth / 2, HexagonsHeight / 2);
        Vector2I centerAxial = HexUtils.OffsetToAxial(centerOffset);
        float baseRadius = Math.Min(HexagonsWidth, HexagonsHeight) / 2f;

        for (int x = 0; x < HexagonsWidth; x++)
        {
            for (int y = 0; y < HexagonsHeight; y++)
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

    /// <summary>
    /// Generates a dictionary of axial coordinates mapped to zone types for the
    /// map using the configured seed.
    /// </summary>
    /// <returns>Dictionary of axial coordinates to zone types.</returns>
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
                < 0.6f => Enums.ZoneType.Safe,
                < 0.75f => Enums.ZoneType.Dangerous,
                < 0.9f => Enums.ZoneType.Water,
                _ => Enums.ZoneType.Unpassable,
            };

            mapData[axial] = type;
        }

        PlaceTransitions(mapData);

        return mapData;
    }

    private void PlaceTransitions(Dictionary<Vector2I, Enums.ZoneType> mapData)
    {
        TransitionTiles.Clear();

        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Seed;

        List<Vector2I> north = new();
        List<Vector2I> south = new();
        List<Vector2I> west = new();
        List<Vector2I> east = new();

        foreach (var kvp in mapData)
        {
            if (kvp.Value == Enums.ZoneType.Unpassable || kvp.Value == Enums.ZoneType.Water)
                continue;

            var offset = HexUtils.AxialToOffset(kvp.Key);

            if (offset.Y == 0)
                north.Add(offset);
            if (offset.Y == HexagonsHeight - 1)
                south.Add(offset);
            if (offset.X == 0)
                west.Add(offset);
            if (offset.X == HexagonsWidth - 1)
                east.Add(offset);
        }

        if (north.Count > 0)
            TransitionTiles[north[rng.RandiRange(0, north.Count - 1)]] = "north";
        if (south.Count > 0)
            TransitionTiles[south[rng.RandiRange(0, south.Count - 1)]] = "south";
        if (west.Count > 0)
            TransitionTiles[west[rng.RandiRange(0, west.Count - 1)]] = "west";
        if (east.Count > 0)
            TransitionTiles[east[rng.RandiRange(0, east.Count - 1)]] = "east";
    }

    /// <summary>
    /// Places a list of locations on passable tiles within their hinted
    /// quadrant. The method is deterministic with regard to the generator seed.
    /// </summary>
    /// <param name="mapData">Generated zone data.</param>
    /// <param name="locations">Locations to place.</param>
    public void PlaceLocations(Dictionary<Vector2I, Enums.ZoneType> mapData, IList<LocationInfo> locations)
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Seed;

        foreach (var location in locations)
        {
            List<Vector2I> candidates = new List<Vector2I>();

            foreach (var kvp in mapData)
            {
                if (kvp.Value == Enums.ZoneType.Unpassable || kvp.Value == Enums.ZoneType.Water)
                    continue;

                Vector2I offset = HexUtils.AxialToOffset(kvp.Key);

                bool inQuadrant = location.Hint switch
                {
                    DirectionHint.NorthWest => offset.X < HexagonsWidth / 2 && offset.Y < HexagonsHeight / 2,
                    DirectionHint.NorthEast => offset.X >= HexagonsWidth / 2 && offset.Y < HexagonsHeight / 2,
                    DirectionHint.SouthWest => offset.X < HexagonsWidth / 2 && offset.Y >= HexagonsHeight / 2,
                    DirectionHint.SouthEast => offset.X >= HexagonsWidth / 2 && offset.Y >= HexagonsHeight / 2,
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
