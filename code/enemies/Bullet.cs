using System;
using Sandbox;

namespace DeadLines;


public class Bullet : Enemy
{
	public override int AddScore { get; set; } = 0;
	public override float BaseHealth { get; set; } = 1f;

	public override Color Color { get; set; } = Color.Yellow;

	public float Speed { get; set; } = 20000f;
	public TimeUntil LifeEnd { get; set; } = 5f;


	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/vector/circle.vmdl" );

		Scale = 0.6f;
		SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 32f / Scale );
	}


	public override void Tick()
	{
		base.Tick();

		if ( LifeEnd && !Destroyed )
		{
			Destroy();
		}

		Velocity = (Rotation.Forward * Speed) * Time.Delta;
		Position = (Position + (Velocity * Time.Delta)).WithZ( 0 );
	}

	public override void Knockback( Vector3 vel )
	{
		base.Knockback( vel / Scale );
	}
}