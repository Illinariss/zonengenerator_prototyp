// Scripts/Map/MapGenerator.cs
using Godot;
using System;
using System.Collections.Generic;


public partial class MapGenerator : Node
{
    public int Width = 20;
    public int Height = 20;
    public int Seed = 12345;

    public Dictionary<Vector2I, Enums.ZoneType> Generate()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Seed;

        Dictionary<Vector2I, Enums.ZoneType> mapData = new Dictionary<Vector2I, Enums.ZoneType>();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Vector2I offset = new Vector2I(x, y);
                Vector2I axial = HexUtils.OffsetToAxial(offset);

                float roll = rng.Randf();
                Enums.ZoneType type = roll switch
                {
                    < 0.7f => Enums.ZoneType.Safe,
                    < 0.9f => Enums.ZoneType.Dangerous,
                    _ => Enums.ZoneType.Unpassable,
                };

                mapData[axial] = type;
            }
        }

        // Beispiel: Übergang nach Norden erzwingen
        for (int i = 0; i < Width; i++)
        {
            var pos = HexUtils.OffsetToAxial(new Vector2I(i, 0));
            mapData[pos] = Enums.ZoneType.Safe; // oder ein spezieller Übergangstyp
        }

        return mapData;
    }
}
