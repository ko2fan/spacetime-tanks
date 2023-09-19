using Godot;
using System;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    private Control ui;
    private Control control;

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
    }

    public override void _Ready()
    {
        ui = GetNode<Control>("/root/Game/UI");
        control = GetNode<Control>("/root/Game/Control");
    }

    public void network_loaded()
    {
        control.Hide();
        ui.Show();
    }
}
