using Godot;
using System;
using System.Collections.Generic;
using SpacetimeDB.Types;

public partial class arena : Node2D
{
    private bool isOwner = false;
    private List<player_tank> tanks = new List<player_tank>();
    private Node spawnLocations;
    private Vector2 chosenSpawnLocation;
    private PackedScene tankScene;
    
    public override void _Ready()
    {
        spawnLocations = GetNode<Node>("SpawnLocations");
        tankScene = GD.Load<PackedScene>("res://Scenes/player_tank.tscn");
        foreach(Node2D node in spawnLocations.GetChildren())
        {
            chosenSpawnLocation = node.Position;
        }
        SpawnTank(true);
    }

    public void SetOwner() => isOwner = true;

    void SpawnTank(bool controlled)
    {
        player_tank tank = tankScene.Instantiate<player_tank>();
        if (controlled)
            tank.SetControlled();
        tanks.Add(tank);
        tank.GlobalPosition = chosenSpawnLocation;
        AddChild(tank);
        Reducer.CreatePlayer("tank1");
    }
}
