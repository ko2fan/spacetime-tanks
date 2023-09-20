using Godot;
using SpacetimeDB.Types;

public partial class player_tank : CharacterBody2D
{
	public const float Speed = 300.0f;
	private StdbVector2 tankPosition = new StdbVector2();
	private StdbVector2 tankDirection = new StdbVector2();
	private bool isControlled = false;
	private ulong entityId;
	private PackedScene bulletScene;
	private double timeBetweenShots = 0.5;
	private double lastShot = 9999;

	public override void _Ready()
	{
		base._Ready();
		
		bulletScene = GD.Load<PackedScene>("res://Scenes/bullet.tscn");
		
		MobileLocationComponent.OnUpdate += MobileLocationComponent_OnUpdate;
		BulletComponent.OnInsert += BulletComponent_OnInsert;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!isControlled)
			return;
		Vector2 velocity = Velocity;

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Y = direction.Y * Speed;
			LookAt(Position + direction);
			
			tankDirection.X = direction.X;
			tankDirection.Z = direction.Y;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed);
		}

		if (Input.IsActionPressed("shoot"))
		{
			if (timeBetweenShots < lastShot)
			{
				Reducer.ShootBullet(tankPosition, tankDirection);
				lastShot = 0;
			}
		}

		lastShot += delta;

		Velocity = velocity;
		MoveAndSlide();

		tankPosition.X = Position.X;
		tankPosition.Z = Position.Y;
		
		Reducer.MovePlayer(tankPosition, tankDirection);
	}

	public void SetControlled()
	{
		isControlled = true;
		GD.Print("Set as local player");
	}

	public void SetEntityId(ulong entity)
	{
		entityId = entity;
	}

	void MobileLocationComponent_OnUpdate(MobileLocationComponent oldValue, MobileLocationComponent newValue,
		ReducerEvent reducerEvent)
	{
		if(newValue.EntityId == entityId)
		{
			// update the Tank position and direction
			tankPosition = newValue.Location;
			tankDirection = newValue.Direction;

			Position = new Vector2(tankPosition.X, tankPosition.Z);
			Vector2 direction = new Vector2(tankDirection.X, tankDirection.Z);
			LookAt(Position + direction);
		}
	}

	void BulletComponent_OnInsert(BulletComponent bulletComponent, ReducerEvent reducerEvent)
	{
		// Spawn bullet here
		Bullet bullet = bulletScene.Instantiate<Bullet>();
		bullet.SetEntityId(bulletComponent.EntityId);
		GetParent().AddChild(bullet);
	}
}
