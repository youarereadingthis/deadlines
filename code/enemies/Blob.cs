using System;
using Sandbox;

namespace DeadLines;


public class Blob : Enemy
{
	public override int AddScore { get; set; } = 1;
	public override float BaseHealth { get; set; } = 0.5f;

	public override float Acceleration { get; set; } = 400f;
	public override float Drag { get; set; } = 0.5f;

	public override Color Color { get; set; } = new Color( .91f, .29f, .117f );

	public override string HitSound { get; set; } = "hit2";

	public float DefaultScale { get; set; } = 3.0f;


	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/vector/circle.vmdl" );
		SetScale( DefaultScale );
	}


	public void SetScale( float scale )
	{
		Scale = scale;
		SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 32f);

		Health = BaseHealth * scale;
	}

	public bool SplitBlob()
	{
		if ( Scale < 2f )
			return false;

		var blobCount = 2;
		var blobScale = Scale / blobCount;
		var angDiff = 360 / blobCount;

		var rot = Rotation.From( new Angles( 0f, Random.Shared.Float( 0f, 360f ), 0f ) );

		for ( int i = 0; i < blobCount; i++ )
		{
			var dir = rot.Forward;
			var b = new Blob();
			b.SetScale( blobScale );
			b.Position = Position + (dir * 32f * blobScale);
			b.Velocity = Velocity + (dir * 100f);

			rot = rot.RotateAroundAxis( Vector3.Up, angDiff );
		}

		return true;
	}


	public override void Tick()
	{
		base.Tick();

		if ( !Destroyed )
		{
			if ( ValidTarget() )
				TargetPos = Player.Position.WithZ( 0 );

			var dir = (TargetPos - Position).Normal;

			Velocity += (dir * Acceleration) * Time.Delta;
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
		if ( !cleanup && SplitBlob() )
			Delete();

		base.Destroy( cleanup );
	}
}