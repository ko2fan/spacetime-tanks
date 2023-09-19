using Godot;
using SpacetimeDB.Types;

public partial class player_tank : CharacterBody2D
{
	public const float Speed = 300.0f;
	private StdbVector2 tankPosition = new StdbVector2();
	private StdbVector2 tankDirection = new StdbVector2();
	private bool isControlled = false;

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

		Velocity = velocity;
		MoveAndSlide();

		tankPosition.X = Position.X;
		tankPosition.Z = Position.Y;
		
		Reducer.MovePlayer(tankPosition, tankDirection);
	}

	public void SetControlled()
	{
		isControlled = true;
	}
}
