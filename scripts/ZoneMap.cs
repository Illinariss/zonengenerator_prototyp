using System.Collections.Generic;

/// <summary>
/// Data object describing the parameters for a generated map.
/// </summary>
public class ZoneMap
{
    /// <summary>Number of tiles horizontally.</summary>
    public int HexagonsWidth { get; set; }

    /// <summary>Number of tiles vertically.</summary>
    public int HexagonsHeight { get; set; }

    /// <summary>Width of the world in kilometres.</summary>
    public float WorldWidthKm { get; set; }

    /// <summary>Height of the world in kilometres.</summary>
    public float WorldHeightKm { get; set; }

    /// <summary>Seed used for deterministic map generation.</summary>
    public int Seed { get; set; }

    /// <summary>Locations that should appear on the map.</summary>
    public IList<LocationInfo> Locations { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneMap"/> class.
    /// </summary>
    /// <param name="width">Number of tiles horizontally.</param>
    /// <param name="height">Number of tiles vertically.</param>
    /// <param name="worldWidthKm">Width of the world in kilometres.</param>
    /// <param name="worldHeightKm">Height of the world in kilometres.</param>
    /// <param name="seed">Random seed for map generation.</param>
    /// <param name="locations">Locations that should appear on the map.</param>
    public ZoneMap(int width, int height, float worldWidthKm, float worldHeightKm, int seed, IList<LocationInfo> locations)
    {
        HexagonsWidth = width;
        HexagonsHeight = height;
        WorldWidthKm = worldWidthKm;
        WorldHeightKm = worldHeightKm;
        Seed = seed;
        Locations = locations;
    }
}
