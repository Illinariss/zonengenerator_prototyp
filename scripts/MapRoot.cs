using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Root node for a generated hex map. Handles tile maps, fog of war and
/// exposes events for player interactions.
/// </summary>
public partial class MapRoot : Node2D
{
    /// <summary>Number of horizontal tiles.</summary>
    [Export]
    public int width = 100;

    /// <summary>Number of vertical tiles.</summary>
    [Export]
    public int height = 60;

    /// <summary>Width of the world in kilometres.</summary>
    [Export]
    public float WorldWidthKm = 100f;

    /// <summary>Height of the world in kilometres.</summary>
    [Export]
    public float WorldHeightKm = 60f;

    /// <summary>Generator used to create terrain and transitions.</summary>
    [Export]
    public MapGenerator? Generator;

    /// <summary>List of locations that can appear on the map.</summary>
    public IList<LocationInfo>? Locations { get; set; }

    /// <summary>World distance represented by a single tile horizontally.</summary>
    public float KmPerHexX => WorldWidthKm / width;

    /// <summary>World distance represented by a single tile vertically.</summary>
    public float KmPerHexY => WorldHeightKm / height;

    /// <summary>Area in square kilometres represented by a tile.</summary>
    public float KmPerHexArea => KmPerHexX * KmPerHexY;

    TileMapLayer visual, logic, fog, overlay;
    Dictionary<Vector2I, Enums.ZoneType> zoneData = new();
    Dictionary<Vector2I, string> transitions = new();
    Dictionary<Vector2I, LocationInfo> locationLookup = new();
    HashSet<Vector2I> discoveredLocationTiles = new();
    /// <summary>
    /// Tiles that lead to other maps mapped to their destination identifier.
    /// </summary>
    public IReadOnlyDictionary<Vector2I, string> Transitions => transitions;

    readonly HashSet<Vector2I> visitedTiles = new();
    readonly HashSet<Vector2I> discoveredTiles = new();
    float distanceTravelledKm = 0f;
    Vector2I? lastVisitedTile = null;
    /// <summary>Total distance travelled by the character in kilometres.</summary>
    public float DistanceTravelledKm => distanceTravelledKm;

    /// <summary>
    /// Fired whenever the character steps onto a new tile.
    /// </summary>
    public event Action<Vector2I>? OnTileEntered;

    /// <summary>
    /// Fired when a transition tile is entered. The string parameter contains
    /// the destination map name.
    /// </summary>
    public event Action<string>? OnTransitionEntered;

    /// <summary>
    /// Fired when the user right-clicks a tile on the map.
    /// </summary>
    public event Action<Vector2I>? OnTileRightClicked;

    /// <summary>
    /// Fired the first time a location becomes visible to the player.
    /// </summary>
    public event Action<LocationInfo>? OnLocationDiscovered;

    /// <summary>
    /// Initializes tilemap references and spawns the player character when the
    /// node enters the scene tree.
    /// </summary>
    public override void _Ready()
    {
        visual = GetNode<TileMapLayer>("%TileMap_Visual");
        logic = GetNode<TileMapLayer>("%TileMap_Logic");
        fog = GetNode<TileMapLayer>("%TileMap_Fog");
        overlay = GetNode<TileMapLayer>("%TileMap_Overlay");

        OnTransitionEntered += destination => GD.Print($"Load map: {destination}");

        visitedTiles.Clear();
        discoveredTiles.Clear();
        discoveredLocationTiles.Clear();
        distanceTravelledKm = 0f;
        lastVisitedTile = null;

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

    /// <summary>
    /// Converts a viewport position to map tile coordinates if inside the view
    /// port.
    /// </summary>
    /// <param name="screenPos">Mouse position in viewport coordinates.</param>
    /// <param name="tile">Resulting tile coordinates when inside the viewport.</param>
    /// <returns>True if the position lies within the viewport.</returns>
    bool TryGetTileFromScreen(Vector2 screenPos, out Vector2I tile)
    {
        tile = Vector2I.Zero;
        var viewport = GetViewport();
        var rect = new Rect2(Vector2.Zero, viewport.GetVisibleRect().Size);
        if (!rect.HasPoint(screenPos))
            return false;

        Vector2 local = overlay.GetLocalMousePosition();
        tile = overlay.LocalToMap(local);
        return true;
    }

    /// <summary>
    /// Handles mouse interactions such as previewing paths, moving the
    /// character on double-click and notifying about right clicks.
    /// </summary>
    /// <param name="@event">Input event from Godot.</param>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion ev)
        {
            if (!TryGetTileFromScreen(ev.Position, out var tileCoords))
            {
                if (lastTileUnderMouseCoordinates != Vector2I.Zero)
                    ClearPreview();
                return;
            }

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
        else if (@event is InputEventMouseButton mouse && mouse.Pressed)
        {
            if (!TryGetTileFromScreen(mouse.Position, out var tileCoords))
                return;

            if (!usedtiles.Contains(tileCoords))
                return;

            if (mouse.ButtonIndex == MouseButton.Left && mouse.DoubleClick)
            {
                var character = GetNodeOrNull<CharacterNode>("CharacterNode");
                if (character == null)
                    return;

                var startOffset = visual.LocalToMap(character.Position);
                var path = ComputePath(startOffset, tileCoords);

                if (path.Count > 0)
                    _ = AnimateCharacterAlongPath(character, path);
            }
            else if (mouse.ButtonIndex == MouseButton.Right)
            {
                OnTileRightClicked?.Invoke(tileCoords);
            }
        }
    }

    List<Vector2I> usedtiles = new List<Vector2I>();

    /// <summary>
    /// Generates the hex grid using the configured <see cref="Generator"/> and
    /// populates tile maps, transitions and locations.
    /// </summary>
    public void GenerateTerrain()
    {
        usedtiles.Clear();
        visual.Clear();
        logic.Clear();
        fog.Clear();
        overlay.Clear();

        visitedTiles.Clear();
        discoveredTiles.Clear();
        distanceTravelledKm = 0f;
        lastVisitedTile = null;

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

        RandomNumberGenerator rng = new RandomNumberGenerator();

        foreach (var axial in axialCoords)
        {
            var coords = HexUtils.AxialToOffset(axial);
            Vector2I visualTile = zoneData[axial] switch
            {
                Enums.ZoneType.Unpassable => new Vector2I(1, 0),
                Enums.ZoneType.Dangerous => new Vector2I(1, 2),
                Enums.ZoneType.Safe =>
                    rng.RandiRange(0, 1) == 0
                        ? new Vector2I(2, 2)
                        : new Vector2I(3, 2),
                Enums.ZoneType.Water => new Vector2I(0, 2),
                _ => new Vector2I(2, 2)
            };

            visual.SetCell(coords, 0, visualTile);
            overlay.SetCell(coords, 0, new Vector2I(0, 0));
            fog.SetCell(coords, 0, new Vector2I(0, 0));
            logic.SetCell(coords, 0, new Vector2I((int)zoneData[axial], 0));
            usedtiles.Add(coords);
        }

        locationLookup.Clear();
        if (Locations != null)
        {
            foreach (var loc in Locations)
            {
                locationLookup[loc.Coordinates] = loc;
            }
        }
    }

    /// <summary>
    /// Updates the fog of war around the given player position.
    /// </summary>
    /// <param name="playerPosition">Player tile coordinates.</param>
    /// <param name="sightRadius">Number of tiles the player can see.</param>
    public void UpdateFog(Vector2I playerPosition, int sightRadius)
    {
        var centerAxial = HexUtils.OffsetToAxial(playerPosition);
        var visibleHexes = HexUtils.GetHexesInRange(centerAxial, sightRadius);
        var visibleSet = new HashSet<Vector2I>(visibleHexes);

        foreach (var tile in usedtiles)
        {
            var axial = HexUtils.OffsetToAxial(tile);
            int distance = HexUtils.Distance(centerAxial, axial);
            bool visible = visibleSet.Contains(axial);

            if (visible)
            {
                discoveredTiles.Add(tile);
                float ratio = (float)distance / Math.Max(1, sightRadius);
                int index = 4 - Mathf.Clamp(Mathf.FloorToInt(ratio * 4), 0, 4);
                fog.SetCell(tile, 0, new Vector2I(index, 0));
                CheckLocation(axial);
            }
            else if (discoveredTiles.Contains(tile))
            {
                fog.SetCell(tile, 0, new Vector2I(3, 0));
            }
            else
            {
                fog.SetCell(tile, 0, new Vector2I(0, 0));
            }
        }
    }

    /// <summary>
    /// Returns the world position of the center of the specified tile.
    /// </summary>
    /// <param name="tile">Tile coordinates.</param>
    public Vector2 GetTileCenter(Vector2I tile)
    {
        // MapToLocal() already returns the center for hex maps. Adding half the
        // tile size would shift the result down by one row.
        return visual.MapToLocal(tile);
    }

    /// <summary>
    /// Converts a local position to offset tile coordinates using the visual tile map.
    /// </summary>
    public Vector2I LocalToMap(Vector2 position)
    {
        return visual.LocalToMap(position);
    }

    /// <summary>
    /// Determines whether the specified tile has been discovered by the player.
    /// </summary>
    public bool IsTileDiscovered(Vector2I tile)
    {
        return discoveredTiles.Contains(tile);
    }

    /// <summary>
    /// Computes a path in axial coordinates between two offset tiles using BFS.
    /// </summary>
    /// <param name="startOffset">Start tile in offset space.</param>
    /// <param name="targetOffset">Target tile in offset space.</param>
    /// <returns>List of axial coordinates representing the path.</returns>
    public List<Vector2I> ComputePath(Vector2I startOffset, Vector2I targetOffset)
    {
        var startAxial = HexUtils.OffsetToAxial(startOffset);
        var targetAxial = HexUtils.OffsetToAxial(targetOffset);
        return Pathfinder.FindPath(startAxial, targetAxial, ax =>
        {
            return zoneData.ContainsKey(ax) && zoneData[ax] != Enums.ZoneType.Unpassable;
        });
    }

    /// <summary>
    /// Draws the given path on the overlay tile map.
    /// </summary>
    /// <param name="axialPath">Path in axial coordinates.</param>
    public void DrawPath(List<Vector2I> axialPath)
    {
        foreach (var tile in usedtiles)
        {
            overlay.SetCell(tile, 0, new Vector2I(0, 0));
        }

        foreach (var loc in discoveredLocationTiles)
        {
            var offset = HexUtils.AxialToOffset(loc);
            overlay.SetCell(offset, 0, new Vector2I(3, 0));
        }

        foreach (var axial in axialPath)
        {
            var offset = HexUtils.AxialToOffset(axial);
            overlay.SetCell(offset, 0, new Vector2I(1, 0));
        }
    }

    /// <summary>
    /// Animates the character along the supplied axial path.
    /// </summary>
    /// <param name="character">Character to move.</param>
    /// <param name="axialPath">Path in axial coordinates.</param>
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

    /// <summary>
    /// Updates internal state when a tile is entered and fires events.
    /// </summary>
    /// <param name="tile">Tile coordinates.</param>
    public void NotifyTileEntered(Vector2I tile)
    {
        if (lastVisitedTile != null)
        {
            var delta = tile - lastVisitedTile.Value;
            Vector2 diffKm = new Vector2(delta.X * KmPerHexX, delta.Y * KmPerHexY);
            distanceTravelledKm += diffKm.Length();
        }
        visitedTiles.Add(tile);
        lastVisitedTile = tile;

        OnTileEntered?.Invoke(tile);
        if (transitions.TryGetValue(tile, out var destination))
        {
            OnTransitionEntered?.Invoke(destination);
        }

        var axial = HexUtils.OffsetToAxial(tile);
        CheckLocation(axial);
    }

    void CheckLocation(Vector2I axial)
    {
        if (locationLookup.TryGetValue(axial, out var info))
        {
            if (!discoveredLocationTiles.Contains(axial))
            {
                discoveredLocationTiles.Add(axial);
                var offset = HexUtils.AxialToOffset(axial);
                overlay.SetCell(offset, 0, new Vector2I(3, 0));
                OnLocationDiscovered?.Invoke(info);
            }
            else
            {
                var offset = HexUtils.AxialToOffset(axial);
                overlay.SetCell(offset, 0, new Vector2I(3, 0));
            }
        }
    }


}
