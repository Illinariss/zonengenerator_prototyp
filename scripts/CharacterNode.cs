using Godot;
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Node representing the controllable character on the map. It handles
/// movement, fog of war updates and click interactions.
/// </summary>
public partial class CharacterNode : Node2D
{
    /// <summary>
    /// Signal emitted when the character sprite is clicked with the left mouse
    /// button.
    /// </summary>
    [Signal]
    public delegate void CharacterClickedEventHandler(CharacterNode node);

    /// <summary>
    /// Optional texture used for the character sprite.
    /// </summary>
    [Export]
    public Texture2D CharacterImage;

    /// <summary>
    /// Number of hexes the character can see. Used for fog of war updates.
    /// </summary>
    [Export]
    public int SightRadius = 3;
    private Sprite2D _sprite;
    private MapRoot? _mapRoot;
    private Vector2I _mapCoords = Vector2I.Zero;


    /// <summary>
    /// Called by Godot when the node is added to the scene tree. Initializes
    /// sprite, connects input events and updates the fog of war for the initial
    /// position.
    /// </summary>
    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("%CharacterTexture");
        if (CharacterImage != null)
            _sprite.Texture = CharacterImage;
        var body = GetNode<CharacterBody2D>("%CharacterBody2D");
        body.InputEvent += OnBodyInput;
        _mapRoot = GetParentOrNull<MapRoot>();
        if (_mapRoot != null)
        {
            _mapCoords = _mapRoot.LocalToMap(Position);
            _mapRoot.UpdateFog(_mapCoords, SightRadius);
        }
    }

    private void OnBodyInput(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
        {
            EmitSignal(SignalName.CharacterClicked, this);
        }
    }

    private void _on_character_body_2d_mouse_entered()
    {
    }

    private void _on_character_body_2d_mouse_exited()
    {
    }

    /// <summary>
    /// Handles movement input for WASD/arrow keys and updates the character
    /// position on the map when possible.
    /// </summary>
    /// <param name="@event">Input event received from Godot.</param>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            Vector2I delta = Vector2I.Zero;
            switch (key.Keycode)
            {
                case Key.W:
                case Key.Up:
                    delta = new Vector2I(0, -1);
                    break;
                case Key.S:
                case Key.Down:
                    delta = new Vector2I(0, 1);
                    break;
                case Key.A:
                case Key.Left:
                    delta = new Vector2I(-1, 0);
                    break;
                case Key.D:
                case Key.Right:
                    delta = new Vector2I(1, 0);
                    break;
            }

            if (delta != Vector2I.Zero)
            {
                var target = _mapCoords + delta;
                if (_mapRoot != null)
                {
                    var path = _mapRoot.ComputePath(_mapCoords, target);
                    if (path.Count > 0)
                        MoveTo(target);
                }
                else
                {
                    _mapCoords = target;
                    MoveTo(target);
                }
            }
        }
    }

    /// <summary>
    /// Moves the character to the specified map coordinates and updates the fog
    /// of war. If attached to a map, also notifies the map of the movement.
    /// </summary>
    /// <param name="mapCoords">Target tile coordinates in offset space.</param>
    public void MoveTo(Vector2I mapCoords)
    {
        _mapCoords = mapCoords;
        if (_mapRoot != null)
        {
            Position = _mapRoot.GetTileCenter(mapCoords);
            _mapRoot.UpdateFog(mapCoords, SightRadius);
            _mapRoot.NotifyTileEntered(mapCoords);
        }
    }

    /// <summary>
    /// Instantiates the <see cref="CharacterNode"/> from its packed scene.
    /// </summary>
    /// <returns>The newly created node.</returns>
    public static CharacterNode Create()
    {
        var scene = GD.Load<PackedScene>("res://scenes/CharacterNode.tscn");
        var node = scene.Instantiate<CharacterNode>();
        return node;
    }

}
