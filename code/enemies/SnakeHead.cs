using System;
using Sandbox;

namespace DeadLines;


public class SnakeHead : Enemy
{
	public override int AddScore { get; set; } = 3;
	public override float BaseHealth { get; set; } = 2.1f;

	public override float Acceleration { get; set; } = 14f;
	public override float Drag { get; set; } = 1.0f;

	public override Color Color { get; set; } = Color.Green;

	public float WaveOffset { get; set; } = 0f;


	public override void Spawn()
	{
		SetModel( "models/vector/circle.vmdl" );
		Scale = 0.9f;

		SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 32f );

		WaveOffset = Random.Shared.Float( 0, 1024f );

		base.Spawn();
	}

	public void CreateBody( float size = 1.0f )
	{
		Scale *= size;
		Health *= size;

		var dir = (Position - Vector3.Zero).Normal;
		SnakeBody prevBody = null;

		for ( int i = 0; i < 7; i++ )
		{
			var b = new SnakeBody();
			b.Scale *= size;
			b.Health *= size;
			b.Distance *= size;
			b.Position = Position - (dir * i * b.Distance);
			b.Follow = (i == 0) ? this : prevBody;
			prevBody = b;
		}
	}


	public override void Tick()
	{
		base.Tick();

		if ( !Destroyed && ValidTarget() )
		{
			// Log.Info( "ValidTarget() = true" );
			var dir = Rotation.From( (Player.Position - Position).EulerAngles );
			dir = dir.RotateAroundAxis( Vector3.Up, MathF.Cos( (Time.Now + WaveOffset) * 2f ) * 70f );

			Velocity += (dir.Forward * Acceleration) * Time.Delta;
		}

		Velocity -= (Velocity * Drag) * Time.Delta;
		Position += Velocity;
	}

	public override void Knockback( Vector3 vel )
	{
		base.Knockback( vel );
	}
}