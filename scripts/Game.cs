using System.Collections.Generic;
using Godot;

public partial class Game : Control
{
    [Export] public PanelContainer MainContent;

    private Control previousContent;

    public override void _Ready()
    {
        // // Handles only the first child
         previousContent = MainContent.GetChildCount() > 0 ? MainContent.GetChild<Control>(0) : null;

    }    

    internal void _on_btn_kampf_1_pressed()
    {
        

    }



}
