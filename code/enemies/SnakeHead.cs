using System;
using System.Collections.Generic;
using Sandbox;

namespace DeadLines;


public class SnakeHead : Enemy
{
	public override int AddScore { get; set; } = 3;
	public override float BaseHealth { get; set; } = 2.1f;

	public override float Acceleration { get; set; } = 500f;
	public override float Drag { get; set; } = 0.8f;

	public override Color Color { get; set; } = Color.Green;

	public override string HitSound { get; set; } = "hit4";

	public float WaveOffset { get; set; } = 0f;
	public List<SnakeBody> Body { get; set; } = new();


	public override void Spawn()
	{
		SetModel( "models/vector/circle.vmdl" );
		Scale = 1.25f;

		SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 32f );

		WaveOffset = Random.Shared.Float( 0, 1024f );

		base.Spawn();
	}

	public void CreateBody( float size = 1.0f )
	{
		Scale *= size;
		SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 32f );

		Health *= size;

		var dir = -(Position - Vector3.Zero).Normal;
		SnakeBody prevBody = null;

		for ( int i = 0; i < 7; i++ )
		{
			var b = new SnakeBody();
			b.Health *= size;
			b.Distance *= size;
			b.Position = Position - (dir * i * b.Distance);
			b.Follow = (i == 0) ? this : prevBody;

			b.Scale *= size;
			b.SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 32f );

			prevBody = b;
			Body.Add( b );
		}
	}


	public override void Tick()
	{
		base.Tick();

		if ( !Destroyed )
		{
			if ( ValidTarget() )
				TargetPos = Player.Position.WithZ( 0 );

			var dir = Rotation.From( (TargetPos - Position).EulerAngles );
			// dir = dir.RotateAroundAxis( Vector3.Up, MathF.Cos( (Time.Now + WaveOffset) * 2f ) * 70f );
			Position += dir.Right * MathF.Cos( (Time.Now + WaveOffset) * 3f ) * (200f * Scale) * Time.Delta;

			Velocity += (dir.Forward * Acceleration) * Time.Delta;
		}

		Velocity -= (Velocity * Drag) * Time.Delta;
		Position = (Position + (Velocity * Time.Delta)).WithZ( 0 );
	}

	public override void Knockback( Vector3 vel )
	{
		base.Knockback( vel / Scale );
	}

	public override void Destroy( bool cleanup = false )
	{
		base.Destroy();

		foreach ( var b in Body )
		{
			if ( b.IsValid && !b.Destroyed )
				b.Destroy();
		}
	}
}