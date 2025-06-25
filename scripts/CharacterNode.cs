using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class CharacterNode : Node2D
{
    [Signal]
    public delegate void CharacterClickedEventHandler(CharacterNode node);
    [Export] public Texture2D CharacterImage;
    private Sprite2D _sprite;
    private bool _isHovered = false;
    private bool _isCurrentFighter = false;    
   
    private ShaderMaterial _shadermaterial;


    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("%CharacterTexture");        
        if (CharacterImage != null)
            _sprite.Texture = CharacterImage;
        var body = GetNode<CharacterBody2D>("%CharacterBody2D");
        body.InputEvent += OnBodyInput;
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

    public void SetCurrentFighter(bool value)
    {
        _isCurrentFighter = value;
    } 
    
    public static CharacterNode Create()
    {
        var scene = GD.Load<PackedScene>("res://Scripts/battlesystem/CharacterNode.tscn");
        var node = scene.Instantiate<CharacterNode>();
        return node;
    }

}
