using Godot;
using SpacetimeDB;
using System.Collections.Generic;
using SpacetimeDB.Types;

public partial class SpacetimeDBManager : Node
{
	private Identity local_identity;

	const string HOST = "localhost:3000";
	const string DBNAME = "spacetime-tanks";
	const bool SSL_ENABLED = false;

	public override void _Ready()
	{
		AuthToken.Init(".spacetime_tanks");

		SpacetimeDBClient.CreateInstance(new ConsoleLogger());

		SpacetimeDBClient.instance.onConnect += () =>
        {
            GD.Print("Connected.");

            SpacetimeDBClient.instance.Subscribe(new List<string>()
            {
	            "SELECT * FROM SpawnableEntityComponent",
	            "SELECT * FROM PlayerComponent",
	            "SELECT * FROM MobileLocationComponent",
	            "SELECT * FROM BulletComponent",
            });
            
        };

        // called when we have an error connecting to SpacetimeDB
        SpacetimeDBClient.instance.onConnectError += (error, message) =>
        {
            GD.PrintErr($"Connection error: " + (error.HasValue ? error.Value.ToString() : "Null") + " - " + message);
        };

        // called when we are disconnected from SpacetimeDB
        SpacetimeDBClient.instance.onDisconnect += (closeStatus, error) =>
        {
            GD.Print("Disconnected.");
        };
        
        // called when we receive the client identity from SpacetimeDB
        SpacetimeDBClient.instance.onIdentityReceived += (token, identity) => {
            AuthToken.SaveToken(token);
            local_identity = identity;
        };
        
        SpacetimeDBClient.instance.onSubscriptionApplied += OnSubscriptionApplied;
        PlayerComponent.OnInsert += PlayerComponent_OnInsert;
        PlayerComponent.OnUpdate += PlayerComponent_OnUpdate;

		SpacetimeDBClient.instance.Connect(AuthToken.Token, HOST, DBNAME, SSL_ENABLED);
	}

	public override void _Process(double delta)
	{
		SpacetimeDBClient.instance.Update();
	}
	
	void OnSubscriptionApplied()
	{
		// If this is the first time connecting, then create player
		var player = PlayerComponent.FilterByOwnerId(local_identity);
		if (player == null)
		{
			// Only create the player if not already there
			Reducer.CreatePlayer(local_identity.ToString()!.Substring(0, 8));
		}
		GameManager.Instance.network_loaded();
		
		// Now that we've done this work we can unregister this callback
		SpacetimeDBClient.instance.onSubscriptionApplied -= OnSubscriptionApplied;
	}

	void PlayerComponent_OnInsert(PlayerComponent playerComponent, ReducerEvent reducerEvent)
	{
		PlayerComponentChanged(playerComponent, reducerEvent);
	}

	void PlayerComponent_OnUpdate(PlayerComponent oldPlayerComponent, PlayerComponent newPlayerComponent,
		ReducerEvent reducerEvent)
	{
		PlayerComponentChanged(newPlayerComponent, reducerEvent);
	}

	void PlayerComponentChanged(PlayerComponent playerComponent, ReducerEvent reducerEvent)
	{
		// if the identity of the PlayerComponent matches our user identity then this is the local player
		if(playerComponent.OwnerId == local_identity)
		{
			// Set local player data
			GameManager.Instance.AddPlayer(true, playerComponent.EntityId);
		}
		// otherwise this is a remote player
		else
		{
			// spawn the remote player's tank
			GameManager.Instance.AddPlayer(false, playerComponent.EntityId);
		}
	}
}
