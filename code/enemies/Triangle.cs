using System;
using Sandbox;

namespace DeadLines;


public class Triangle : Enemy
{
	public override int AddScore { get; set; } = 2;
	public override float BaseHealth { get; set; } = 1.0f;

	public override float Acceleration { get; set; } = 900f;
	public override float Drag { get; set; } = 1.0f;

	public override Color Color { get; set; } = Color.Yellow;

	public override string HitSound { get; set; } = "hit3";

	public float TurnSpeed { get; set; } = 2.0f;
	public Rotation Direction { get; set; }


	public override void Spawn()
	{
		SetModel( "models/vector/triangle.vmdl" );
		Scale = 0.9f;

		var hull = new BBox( Vector3.Zero, 64f );
		SetupPhysicsFromOBB( PhysicsMotionType.Keyframed, hull.Mins, hull.Maxs );

		Rotation = Rotation.LookAt( (Vector3.Zero - Position.WithZ( 0 )).Normal, Vector3.Up );

		base.Spawn();
	}


	public override void Tick()
	{
		base.Tick();

		if ( !Destroyed && ValidTarget() )
		{
			Direction = Rotation.LookAt( (Player.Position.WithZ( 0 ) - Position.WithZ( 0 )).Normal, Vector3.Up );
			Rotation = Rotation.Slerp( Rotation, Direction, Time.Delta * TurnSpeed, true );

			Velocity += (Rotation.Forward * Acceleration) * Time.Delta;
		}

		Velocity -= (Velocity * Drag) * Time.Delta;
		Position = (Position + (Velocity * Time.Delta)).WithZ( 0 );
	}
}
