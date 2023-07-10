using System;
using Sandbox;

namespace DeadLines;


public class SnakeBody : Enemy
{
	public override int AddScore { get; set; } = 0;
	public override float BaseHealth { get; set; } = 2f;
	public override Color Color { get; set; } = Color.Green;

	public Enemy Follow { get; set; } = null;
	public static float Distance { get; set; } = 50f;


	public override void Spawn()
	{
		SetModel( "models/vector/circle.vmdl" );
		SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 32f );
		Scale = 0.7f;

		base.Spawn();
	}


	public override void Tick()
	{
		base.Tick();

		if ( Follow.IsValid() )
		{
			var dist = Follow.Position.Distance( Position );
			if ( dist > Distance )
			{
				var dir = (Position - Follow.Position).Normal;
				Position = Follow.Position + (dir * Distance);
			}
		}
		else
		{
			Destroy();
		}

		Velocity -= (Velocity * Drag) * Time.Delta;
		Position += Velocity;
	}

	public override void Knockback( Vector3 vel )
	{
		base.Knockback( vel );
	}
}