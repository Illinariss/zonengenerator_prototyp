using Godot;
using System;

public partial class MapCameraController : Camera2D
{
    [Export] public int ScrollBorder = 20;
    [Export] public float ScrollSpeed = 400f;
    [Export] public MapRoot? Map;
    [Export] public NodePath CharacterPath = "";
    [Export] public NodePath RecenterButtonPath = "";

    private CharacterNode? _character;
    private Button? _recenterButton;

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

    public override void _Process(double delta)
    {
        if (Map == null)
            return;

        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector2 dir = Vector2.Zero;

        if (mousePos.X < ScrollBorder)
            dir.X -= 1;
        else if (mousePos.X > viewportSize.X - ScrollBorder)
            dir.X += 1;

        if (mousePos.Y < ScrollBorder)
            dir.Y -= 1;
        else if (mousePos.Y > viewportSize.Y - ScrollBorder)
            dir.Y += 1;

        if (dir != Vector2.Zero)
        {
            Position += dir.Normalized() * ScrollSpeed * (float)delta;
            ClampToMap(viewportSize);
        }

        if (_character != null && _recenterButton != null)
        {
            Rect2 visible = new Rect2(GlobalPosition - viewportSize / 2, viewportSize);
            bool inside = visible.HasPoint(_character.GlobalPosition);
            if (inside)
                _recenterButton.Hide();
            else
                _recenterButton.Show();
        }
    }

    private void RecenterOnCharacter()
    {
        if (_character == null)
            return;
        Position = _character.GlobalPosition;
        ClampToMap(GetViewportRect().Size);
    }

    private void ClampToMap(Vector2 viewportSize)
    {
        if (Map == null)
            return;

        int mapWidth = Map.Generator != null ? Map.Generator.Width : Map.width;
        int mapHeight = Map.Generator != null ? Map.Generator.Height : Map.height;
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
