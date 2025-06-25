using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class CharacterNode : Node2D
{
    [Signal]
    public delegate void CharacterClickedEventHandler(CharacterNode node);
    [Export] public Texture2D CharacterImage;
    [Export] public int SightRadius = 3;
    private Sprite2D _sprite;
    private bool _isHovered = false;
    private bool _isCurrentFighter = false;    
   
    private ShaderMaterial _shadermaterial;
    private MapRoot? _mapRoot;
    private Vector2I _mapCoords = Vector2I.Zero;


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
        _isHovered = true;
    }

    private void _on_character_body_2d_mouse_exited()
    {
        _isHovered = false;
    }

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
                _mapCoords += delta;
                MoveTo(_mapCoords);
            }
        }
    }

    public void MoveTo(Vector2I mapCoords)
    {
        if (_mapRoot != null)
        {
            Position = _mapRoot.GetTileCenter(mapCoords);
            _mapRoot.UpdateFog(mapCoords, SightRadius);
            _mapRoot.NotifyTileEntered(mapCoords);
        }
    }

    public void SetCurrentFighter(bool value)
    {
        _isCurrentFighter = value;
    } 
    
    public static CharacterNode Create()
    {
        var scene = GD.Load<PackedScene>("res://scenes/CharacterNode.tscn");
        var node = scene.Instantiate<CharacterNode>();
        return node;
    }

}
