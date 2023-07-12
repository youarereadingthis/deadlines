using System;
using Sandbox;

namespace DeadLines;


public class Square : Enemy
{
	public override int AddScore { get; set; } = 2;
	public override float BaseHealth { get; set; } = 2f;

	public override float Acceleration { get; set; } = 900f;
	public override float Drag { get; set; } = 1.0f;

	public override Color Color { get; set; } = Color.Cyan;

	public override string HitSound { get; set; } = "hit1";

	public int SpinDir { get; set; } = 1;
	public float SpinRate { get; set; } = 0.8f;


	public override void Spawn()
	{
		SetModel( "models/vector/square.vmdl" );
		Scale = 1.0f;//1.5f;

		var hull = new BBox( Vector3.Zero, 64f );
		SetupPhysicsFromOBB( PhysicsMotionType.Keyframed, hull.Mins, hull.Maxs );

		SpinDir = (Random.Shared.Int( 0, 1 ) == 1) ? -1 : 1;

		base.Spawn();
	}


	public override void Tick()
	{
		base.Tick();

		// Log.Info( "Square.FollowPlayer()" );

		if ( !Destroyed && ValidTarget() )
		{
			// Log.Info( "ValidTarget() = true" );
			var dir = (Player.Position - Position).Normal;

			Velocity += (dir * Acceleration) * Time.Delta;
		}

		Rotation = Rotation.RotateAroundAxis( Vector3.Up, Velocity.Length * SpinDir * SpinRate * Time.Delta );

		Velocity -= (Velocity * Drag) * Time.Delta;
		Position = (Position + (Velocity * Time.Delta)).WithZ( 0 );
	}

	public override void Knockback( Vector3 vel )
	{
		base.Knockback( vel );
		SpinDir *= -1;
	}
}