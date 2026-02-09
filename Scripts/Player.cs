using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float Speed = 100.0f;
	
	private Camera2D camera => GetNode<Camera2D>("Camera2D");
	
	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		
		Vector2 direction = Input.GetVector("Left", "Right", "Up", "Down");
		
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Y = direction.Y * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed);
		}
		
		Velocity = velocity;
		MoveAndSlide();
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.IsPressed())
		{
			if (mb.ButtonIndex == MouseButton.WheelUp)
			{
				camera.Zoom += new Vector2(0.1f, 0.1f);
			}
			else if (mb.ButtonIndex == MouseButton.WheelDown)
			{
				camera.Zoom -= new Vector2(0.1f, 0.1f);
			}
		}
	}
}
