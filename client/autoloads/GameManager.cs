using Godot;
using System.Collections.Generic;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    private Control ui;
    private Control control;
    private List<Player> playerList = new List<Player>();
    private bool gameStarted;

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

    public void AddPlayer(bool localPlayer, ulong entityId)
    {
        Player player = new Player();
        player.SetEntityId(entityId);
        if (localPlayer)
        {
            player.SetLocal();
        }
        playerList.Add(player);

        if (gameStarted)
        {
            var arena = GetNode<Arena>("/root/Game/Arena");
            arena.SpawnTank(localPlayer, entityId);
            GD.Print("Spawned new tank");
        }
    }

    public void StartGame()
    {
        gameStarted = true;
    }

    public List<Player> GetPlayerList()
    {
        return playerList;
    }
}
