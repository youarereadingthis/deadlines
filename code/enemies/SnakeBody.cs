using System;
using Sandbox;

namespace DeadLines;


public class SnakeBody : Enemy
{
	public override bool AutoDetect { get; set; } = false;

	public override int AddScore { get; set; } = 0;
	public override float BaseHealth { get; set; } = 1.5f;
	public override float Drag { get; set; } = 0.8f;
	public override Color Color { get; set; } = Color.Green;

	public override string HitSound { get; set; } = "hit2";

	public Enemy Follow { get; set; } = null;
	public float Distance { get; set; } = 45f;


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

		Velocity -= (Velocity * Drag) * Time.Delta;
		Position = (Position + (Velocity * Time.Delta)).WithZ( 0 );

		if ( Follow.IsValid() )
		{
			var dist = Follow.Position.Distance( Position );
			if ( dist > Distance )
			{
				var dir = (Position - Follow.Position).Normal;
				Position = Follow.Position + (dir * Distance);
			}
		}
		else if ( !Destroyed )
		{
			Destroy();
		}

	}

	public override void Knockback( Vector3 vel )
	{
		base.Knockback( vel );
	}

	public override void Destroy()
	{
		base.Destroy();

		Follow = null;
	}
}