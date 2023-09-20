using Godot;
using System.Collections.Generic;

public partial class Arena : Node2D
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
        StartGame();
    }

    public void StartGame()
    {
        foreach (Player player in GameManager.Instance.GetPlayerList())
        {
            GD.Print("Adding player");
            SpawnTank(player.GetLocal(), player.GetEntityId());
        }
    }

    public void SpawnTank(bool controlled, ulong EntityId)
    {
        player_tank tank = tankScene.Instantiate<player_tank>();
        if (controlled)
            tank.SetControlled();
        tank.SetEntityId(EntityId);
        tanks.Add(tank);
        tank.GlobalPosition = chosenSpawnLocation;
        AddChild(tank);
    }
}
