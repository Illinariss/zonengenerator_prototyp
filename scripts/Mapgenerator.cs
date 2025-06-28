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
    /// map using the configured seed. Dangerous tiles are placed in small
    /// clusters to create contiguous hazardous areas.
    /// </summary>
    /// <returns>Dictionary of axial coordinates to zone types.</returns>
    public Dictionary<Vector2I, ZoneType> Generate()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Seed;

        Dictionary<Vector2I, ZoneType> mapData = new Dictionary<Vector2I, ZoneType>();

        var shape = GenerateShape();
        shape = ValidateShape(shape);

        foreach (var axial in shape)
        {
            float roll = rng.Randf();
            ZoneType type = roll switch
            {
                < 0.6f => ZoneType.Safe,
                < 0.75f => ZoneType.Water,
                < 0.9f => ZoneType.Unpassable,
                _ => ZoneType.Safe,
            };

            mapData[axial] = type;
        }

        int desiredDanger = (int)(shape.Count * 0.15f);
        List<Vector2I> safeTiles = new List<Vector2I>();
        foreach (var kvp in mapData)
        {
            if (kvp.Value == ZoneType.Safe)
                safeTiles.Add(kvp.Key);
        }

        int clusterCount = Math.Max(1, desiredDanger / 8);
        for (int i = 0; i < clusterCount && safeTiles.Count > 0 && desiredDanger > 0; i++)
        {
            int index = rng.RandiRange(0, safeTiles.Count - 1);
            Vector2I start = safeTiles[index];
            safeTiles.RemoveAt(index);

            int clusterSize = Math.Min(desiredDanger, rng.RandiRange(4, 10));
            Queue<Vector2I> queue = new Queue<Vector2I>();
            queue.Enqueue(start);

            while (queue.Count > 0 && clusterSize > 0)
            {
                Vector2I tile = queue.Dequeue();
                if (!mapData.ContainsKey(tile) || mapData[tile] != ZoneType.Safe)
                    continue;

                mapData[tile] = ZoneType.Dangerous;
                desiredDanger--;
                clusterSize--;

                foreach (var neighbor in HexUtils.GetNeighbors(tile))
                {
                    if (mapData.TryGetValue(neighbor, out var zone) && zone == ZoneType.Safe)
                        queue.Enqueue(neighbor);
                }
            }
        }

        while (desiredDanger > 0 && safeTiles.Count > 0)
        {
            int index = rng.RandiRange(0, safeTiles.Count - 1);
            Vector2I tile = safeTiles[index];
            safeTiles.RemoveAt(index);

            mapData[tile] = ZoneType.Dangerous;
            desiredDanger--;
        }

        RemoveIsolatedTiles(mapData);
        PlaceTransitions(mapData);

        return mapData;
    }

    private void PlaceTransitions(Dictionary<Vector2I, ZoneType> mapData)
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
            if (kvp.Value == ZoneType.Unpassable || kvp.Value == ZoneType.Water)
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
    public void PlaceLocations(Dictionary<Vector2I, ZoneType> mapData, IList<LocationInfo> locations)
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Seed;

        foreach (var location in locations)
        {
            List<Vector2I> candidates = new List<Vector2I>();

            foreach (var kvp in mapData)
            {
                if (kvp.Value == ZoneType.Unpassable || kvp.Value == ZoneType.Water)
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

    /// <summary>
    /// Filters generated axial coordinates so that all resulting tiles lie within
    /// the configured map boundaries.
    /// </summary>
    /// <param name="shape">Generated axial coordinates.</param>
    /// <returns>New list containing only valid coordinates.</returns>
    private List<Vector2I> ValidateShape(List<Vector2I> shape)
    {
        List<Vector2I> valid = new();
        foreach (var axial in shape)
        {
            var offset = HexUtils.AxialToOffset(axial);
            if (offset.X >= 0 && offset.X < HexagonsWidth && offset.Y >= 0 && offset.Y < HexagonsHeight)
                valid.Add(axial);
        }

        return valid;
    }

    /// <summary>
    /// Removes passable tiles that cannot be reached because they are fully surrounded
    /// by unpassable or missing tiles.
    /// </summary>
    /// <param name="mapData">Zone dictionary to modify.</param>
    private void RemoveIsolatedTiles(Dictionary<Vector2I, ZoneType> mapData)
    {
        Vector2I? start = null;
        foreach (var kvp in mapData)
        {
            if (kvp.Value != ZoneType.Unpassable && kvp.Value != ZoneType.Water)
            {
                start = kvp.Key;
                break;
            }
        }

        if (start == null)
            return;

        HashSet<Vector2I> reachable = new();
        Queue<Vector2I> queue = new();
        queue.Enqueue(start.Value);
        reachable.Add(start.Value);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in HexUtils.GetNeighbors(current))
            {
                if (!mapData.TryGetValue(neighbor, out var zone))
                    continue;
                if (zone == ZoneType.Unpassable || zone == ZoneType.Water)
                    continue;
                if (reachable.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        List<Vector2I> keys = new(mapData.Keys);
        foreach (var key in keys)
        {
            var zone = mapData[key];
            if (zone == ZoneType.Unpassable || zone == ZoneType.Water)
                continue;
            if (!reachable.Contains(key))
                mapData.Remove(key);
        }
    }
}
