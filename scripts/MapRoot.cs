using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MapRoot : Node2D
{
    [Export] public int width = 100;
    [Export] public int height = 60;
    [Export] public float WorldWidthKm = 100f;
    [Export] public float WorldHeightKm = 60f;
    [Export] public MapGenerator? Generator;

    public float KmPerHexX => WorldWidthKm / width;
    public float KmPerHexY => WorldHeightKm / height;
    public float KmPerHexArea => KmPerHexX * KmPerHexY;

    TileMapLayer visual, logic, fog, overlay;
    Dictionary<Vector2I, Enums.ZoneType> zoneData = new();
    Dictionary<Vector2I, string> transitions = new();
    public IReadOnlyDictionary<Vector2I, string> Transitions => transitions;

    public event Action<Vector2I>? OnTileEntered;
    public event Action<string>? OnTransitionEntered;

    public override void _Ready()
    {
        visual = GetNode<TileMapLayer>("%TileMap_Visual");
        logic = GetNode<TileMapLayer>("%TileMap_Logic");
        fog = GetNode<TileMapLayer>("%TileMap_Fog");
        overlay = GetNode<TileMapLayer>("%TileMap_Overlay");

        OnTransitionEntered += destination => GD.Print($"Load map: {destination}");

        GenerateTerrain();

        if (transitions.Count > 0)
        {
            var entryTile = transitions.Keys.First();
            var character = CharacterNode.Create();
            character.Name = "CharacterNode";
            AddChild(character);
            character.CallDeferred(nameof(CharacterNode.MoveTo), entryTile);

            var cam = GetNodeOrNull<MapCameraController>("Camera2D");
            if (cam != null)
            {
                cam.SetCharacter(character);
                cam.CallDeferred(nameof(MapCameraController.CenterOnCharacter));
            }
        }
    }

    Vector2I lastTileUnderMouseCoordinates;

    void ClearPreview()
    {
        DrawPath(new List<Vector2I>());
        lastTileUnderMouseCoordinates = Vector2I.Zero;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion ev)
        {
            var tileCoords = overlay.LocalToMap(overlay.ToLocal(ev.GlobalPosition));

            if (!usedtiles.Contains(tileCoords))
            {
                if (lastTileUnderMouseCoordinates != Vector2I.Zero)
                    ClearPreview();
                return;
            }

            if (tileCoords == lastTileUnderMouseCoordinates)
                return;

            lastTileUnderMouseCoordinates = tileCoords;

            var character = GetNodeOrNull<CharacterNode>("CharacterNode");
            if (character == null)
            {
                ClearPreview();
                return;
            }

            var startOffset = visual.LocalToMap(character.Position);
            var path = ComputePath(startOffset, tileCoords);

            if (path.Count > 0)
            {
                DrawPath(path);
                overlay.SetCell(tileCoords, 0, new Vector2I(2, 0));
            }
            else
            {
                ClearPreview();
            }
        }
    }

    List<Vector2I> usedtiles = new List<Vector2I>();

    public void GenerateTerrain()
    {
        visual.Clear();
        logic.Clear();
        fog.Clear();
        overlay.Clear();

        float mapRatio = (float)width / Mathf.Max(1, height);
        float worldRatio = WorldWidthKm / Mathf.Max(0.0001f, WorldHeightKm);
        float deviation = Mathf.Abs(mapRatio - worldRatio) / mapRatio;
        if (deviation > 0.25f)
            throw new InvalidOperationException("World dimension ratio does not match map aspect ratio");

        // Generate terrain here
        zoneData.Clear();
        transitions.Clear();
        IEnumerable<Vector2I> axialCoords;
        if (Generator != null)
        {
            zoneData = Generator.Generate();
            axialCoords = zoneData.Keys;
            transitions = new Dictionary<Vector2I, string>(Generator.TransitionTiles);
        }
        else
        {
            zoneData = new();
            List<Vector2I> temp = new();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var ax = HexUtils.OffsetToAxial(new Vector2I(x, y));
                    temp.Add(ax);
                    zoneData[ax] = Enums.ZoneType.Safe;
                }
            }
            axialCoords = temp;
        }

        foreach (var axial in axialCoords)
        {
            var coords = HexUtils.AxialToOffset(axial);
            visual.SetCell(coords, 0, new Vector2I(2, 2));
            overlay.SetCell(coords, 0, new Vector2I(0, 0));
            fog.SetCell(coords, 0, new Vector2I(0, 0));
            logic.SetCell(coords, 0, new Vector2I((int)zoneData[axial], 0));
            usedtiles.Add(coords);
        }
    }

    public void UpdateFog(Vector2I playerPosition, int sightRadius)
    {
        var centerAxial = HexUtils.OffsetToAxial(playerPosition);
        var visibleHexes = HexUtils.GetHexesInRange(centerAxial, sightRadius);
        var visibleSet = new HashSet<Vector2I>(visibleHexes);

        foreach (var tile in usedtiles)
        {
            var axial = HexUtils.OffsetToAxial(tile);
            int distance = HexUtils.Distance(centerAxial, axial);
            if (visibleSet.Contains(axial))
            {
                float ratio = (float)distance / Math.Max(1, sightRadius);
                int index = 4 - Mathf.Clamp(Mathf.FloorToInt(ratio * 4), 0, 4);
                fog.SetCell(tile, 0, new Vector2I(index, 0));
            }
            else
            {
                fog.SetCell(tile, 0, new Vector2I(0, 0));
            }
        }
    }

    public Vector2 GetTileCenter(Vector2I tile)
    {
        return visual.MapToLocal(tile) + visual.TileSet.TileSize / 2;
    }

    public Vector2I LocalToMap(Vector2 position)
    {
        return visual.LocalToMap(position);
    }

    public List<Vector2I> ComputePath(Vector2I startOffset, Vector2I targetOffset)
    {
        var startAxial = HexUtils.OffsetToAxial(startOffset);
        var targetAxial = HexUtils.OffsetToAxial(targetOffset);
        return Pathfinder.FindPath(startAxial, targetAxial, ax =>
        {
            return zoneData.ContainsKey(ax) && zoneData[ax] != Enums.ZoneType.Unpassable;
        });
    }

    public void DrawPath(List<Vector2I> axialPath)
    {
        foreach (var tile in usedtiles)
        {
            overlay.SetCell(tile, 0, new Vector2I(0, 0));
        }

        foreach (var axial in axialPath)
        {
            var offset = HexUtils.AxialToOffset(axial);
            overlay.SetCell(offset, 0, new Vector2I(1, 0));
        }
    }

    public async System.Threading.Tasks.Task AnimateCharacterAlongPath(CharacterNode character, List<Vector2I> axialPath)
    {
        foreach (var axial in axialPath)
        {
            var offset = HexUtils.AxialToOffset(axial);
            Vector2 target = GetTileCenter(offset);
            var tween = CreateTween();
            tween.TweenProperty(character, "position", target, 0.2f);
            await ToSignal(tween, "finished");
            character.MoveTo(offset);
            OnTileEntered?.Invoke(offset);
        }
    }

    public void NotifyTileEntered(Vector2I tile)
    {
        OnTileEntered?.Invoke(tile);
        if (transitions.TryGetValue(tile, out var destination))
        {
            OnTransitionEntered?.Invoke(destination);
        }
    }


}
