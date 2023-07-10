using System;
using Sandbox;

namespace DeadLines;


public class Square : Enemy
{
	public override int AddScore { get; set; } = 2;
	public override float BaseHealth { get; set; } = 3f;

	public override float Acceleration { get; set; } = 10f;
	public override float Drag { get; set; } = 0.7f;

	public override Color Color { get; set; } = Color.Cyan;

	public int SpinDir { get; set; } = 1;


	public override void Spawn()
	{
		SetModel( "models/vector/square.vmdl" );
		Scale = 1.5f;

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

		Rotation = Rotation.RotateAroundAxis( Vector3.Up, Velocity.Length * SpinDir * 50f * Time.Delta );

		Velocity -= (Velocity * Drag) * Time.Delta;
		Position += Velocity;

		/*var p = TouchingPlayer();
		if ( p != null )
			TouchedPlayer( p );*/
	}

	public override void Knockback( Vector3 vel )
	{
		base.Knockback( vel );
		SpinDir *= -1;
	}

	public override void Destroy()
	{
		base.Destroy();
	}
}