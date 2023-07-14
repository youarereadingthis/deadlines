using System;
using Sandbox;

namespace DeadLines;


public class GateNode : Enemy
{
	public override bool AutoDetect { get; set; } = false;

	public override int AddScore { get; set; } = 2;
	public override float BaseHealth { get; set; } = 1.0f;
	public override float Drag { get; set; } = 4.0f;
	public override Color Color { get; set; } = Color.Orange;

	public override string HitSound { get; set; } = "hit3";

	public Enemy Follow { get; set; } = null;


	public override void Spawn()
	{
		SetModel( "models/vector/circle.vmdl" );

		Scale = 0.65f;
		SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 32f );

		base.Spawn();
	}


	public override void Tick()
	{
		base.Tick();

		if ( Destroyed )
		{
			// Velocity = Vector3.Zero;
			Velocity -= (Velocity * Drag) * Time.Delta;
			Position = (Position + (Velocity * Time.Delta)).WithZ( 0 );

			return;
		}
		else
		{
			Velocity = Vector3.Zero;
		}

		if ( !Follow.IsValid() || Follow.Destroyed )
			Destroy();

	}

	public override void Knockback( Vector3 vel )
	{
		Follow?.Knockback( vel );
		base.Knockback( vel );
	}

	public override void Destroy( bool cleanup = false )
	{
		base.Destroy();

		if ( Follow.IsValid() && !Follow.Destroyed )
			Follow.Destroy();

		Follow = null;
	}
}