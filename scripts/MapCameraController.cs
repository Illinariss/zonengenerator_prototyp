using Godot;
using System;

/// <summary>
/// Camera controller that supports edge scrolling and following a character on
/// the generated map.
/// </summary>
public partial class MapCameraController : Camera2D
{
    /// <summary>Size of the edge-scroll activation zone in pixels.</summary>
    [Export] public int ScrollBorder = 30;

    /// <summary>Speed of manual scrolling in pixels per second.</summary>
    [Export] public float ScrollSpeed = 400f;

    /// <summary>Reference to the map this camera is clamped to.</summary>
    [Export] public MapRoot? Map;

    /// <summary>Path to the character node for auto follow.</summary>
    [Export] public NodePath CharacterPath = "";

    /// <summary>Path to the UI button used to recenter on the character.</summary>
    [Export] public NodePath RecenterButtonPath = "";

    /// <summary>Delay before the camera automatically follows again.</summary>
    [Export] public float AutoFollowDelay = 1f;

    /// <summary>Speed of the automatic follow movement.</summary>
    [Export] public float AutoFollowSpeed = 200f;

    /// <summary>Minimum allowed zoom factor.</summary>
    [Export] public float MinZoom = 0.5f;

    /// <summary>Maximum allowed zoom factor.</summary>
    [Export] public float MaxZoom = 2f;

    private CharacterNode? _character;
    private Button? _recenterButton;
    private float _timeSinceManualScroll = 0f;

    /// <summary>
    /// Sets the character node that the camera can follow.
    /// </summary>
    /// <param name="character">Character node instance.</param>
    public void SetCharacter(CharacterNode character)
    {
        _character = character;
        CharacterPath = GetPathTo(character);
    }

    /// <summary>
    /// Centers the camera on the current character immediately.
    /// </summary>
    public void CenterOnCharacter()
    {
        if (_character == null)
            return;
        Position = _character.GlobalPosition;
        ClampToMap(GetViewportRect().Size / Zoom);
    }

    /// <summary>
    /// Called when the node enters the scene tree. Resolves character and
    /// button references and hooks up callbacks.
    /// </summary>
    public override void _Ready()
    {
        if (Map == null)
            Map = GetParent<MapRoot>();

        if (CharacterPath != null && CharacterPath.ToString() != "")
            _character = GetNodeOrNull<CharacterNode>(CharacterPath);
        else
            _character = GetParent().GetNodeOrNull<CharacterNode>("CharacterNode");

        if (RecenterButtonPath != null && RecenterButtonPath.ToString() != "")
            _recenterButton = GetNodeOrNull<Button>(RecenterButtonPath);

        if (_recenterButton != null)
        {
            _recenterButton.Hide();
            _recenterButton.Pressed += () =>
            {
                RecenterOnCharacter();
                _recenterButton.Hide();
            };
        }
    }

    /// <summary>
    /// Handles edge scrolling and automatic follow behaviour each frame.
    /// </summary>
    /// <param name="delta">Frame time in seconds.</param>
    public override void _Process(double delta)
    {
        if (Map == null)
            return;

        Rect2 viewportRect = GetViewportRect();
        Vector2 viewportSize = viewportRect.Size / Zoom;
        Vector2 screenSize = viewportRect.Size;
        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector2 dir = Vector2.Zero;

        bool inside = viewportRect.HasPoint(mousePos);
        bool mouseVisible = Input.MouseMode == Input.MouseModeEnum.Visible;

        if (inside && mouseVisible)
        {
            if (mousePos.X < ScrollBorder)
                dir.X -= 1;
            else if (mousePos.X > screenSize.X - ScrollBorder)
                dir.X += 1;

            if (mousePos.Y < ScrollBorder)
                dir.Y -= 1;
            else if (mousePos.Y > screenSize.Y - ScrollBorder)
                dir.Y += 1;
        }

        _timeSinceManualScroll += (float)delta;
        if (dir != Vector2.Zero)
        {
            Position += dir.Normalized() * ScrollSpeed * (float)delta;
            ClampToMap(viewportSize);
            _timeSinceManualScroll = 0f;
        }
        else if (_character != null && _timeSinceManualScroll > AutoFollowDelay)
        {
            Position = Position.MoveToward(_character.GlobalPosition, AutoFollowSpeed * (float)delta);
            ClampToMap(viewportSize);
        }

        if (_character != null && _recenterButton != null)
        {
            Rect2 visible = new Rect2(GlobalPosition - viewportSize / 2, viewportSize);
            bool charInside = visible.HasPoint(_character.GlobalPosition);
            if (charInside)
                _recenterButton.Hide();
            else
                _recenterButton.Show();
        }
    }

    /// <summary>
    /// Handles zoom input via the mouse wheel.
    /// </summary>
    /// <param name="@event">Input event from Godot.</param>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse && mouse.Pressed)
        {
            if (mouse.ButtonIndex == MouseButton.WheelUp)
                AdjustZoom(0.1f);
            else if (mouse.ButtonIndex == MouseButton.WheelDown)
                AdjustZoom(-0.1f);
        }
    }

    private void AdjustZoom(float delta)
    {
        float target = Mathf.Clamp(Zoom.X + delta, MinZoom, MaxZoom);
        Zoom = new Vector2(target, target);
        ClampToMap(GetViewportRect().Size / Zoom);
    }

    private void RecenterOnCharacter()
    {
        if (_character == null)
            return;
        Position = _character.GlobalPosition;
        ClampToMap(GetViewportRect().Size / Zoom);
    }

    private void ClampToMap(Vector2 viewportSize)
    {
        if (Map == null)
            return;

        int mapWidth = (Map.Generator != null ? Map.Generator.HexagonsWidth : Map.ZoneMap.HexagonsWidth) + Map.NebulaRingWidth * 2;
        int mapHeight = (Map.Generator != null ? Map.Generator.HexagonsHeight : Map.ZoneMap.HexagonsHeight) + Map.NebulaRingWidth * 2;
        var tilemap = Map.GetNode<TileMapLayer>("%TileMap_Visual");
        Vector2 tileSize = tilemap.TileSet.TileSize;

        Vector2 mapSize = new Vector2(mapWidth * tileSize.X, mapHeight * tileSize.Y);

        float halfW = viewportSize.X / 2f;
        float halfH = viewportSize.Y / 2f;

        float minX = halfW;
        float minY = halfH;
        float maxX = Math.Max(minX, mapSize.X - halfW);
        float maxY = Math.Max(minY, mapSize.Y - halfH);

        Position = new Vector2(
            Mathf.Clamp(Position.X, minX, maxX),
            Mathf.Clamp(Position.Y, minY, maxY)
        );
    }
}
