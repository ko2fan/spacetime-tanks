using Godot;
using SpacetimeDB;
using System.Collections.Generic;

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

		SpacetimeDBClient.instance.Connect(AuthToken.Token, HOST, DBNAME, SSL_ENABLED);
	}

	public override void _Process(double delta)
	{
		SpacetimeDBClient.instance.Update();
	}
	
	void OnSubscriptionApplied()
	{
		GameManager.Instance.network_loaded();
		
		// Now that we've done this work we can unregister this callback
		SpacetimeDBClient.instance.onSubscriptionApplied -= OnSubscriptionApplied;
	}
}
