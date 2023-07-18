using System;
using System.Collections.Generic;
using Sandbox;

namespace DeadLines;


public class DragonHead : Enemy
{
	public override int AddScore { get; set; } = 20;
	public override float BaseHealth { get; set; } = 20f; //100f;
	public float WaveHealthScale { get; set; } = 2f;

	public override float Acceleration { get; set; } = 1300f;
	public override float Drag { get; set; } = 0.5f;

	public override Color Color { get; set; } = Color.Red;

	public override string HitSound { get; set; } = "hit4";

	public float TurnSpeed { get; set; } = 3.0f;

	public int BodyParts { get; set; } = 40;
	public List<DragonBody> Body { get; set; } = new();

	public static TimeUntil NextTarget { get; set; } = 0;
	public static TimeUntil NextSpawn { get; set; } = 10f;
	public float SpawnDelay { get; set; } = 5f;
	public int Wave { get; set; } = 20;


	public override void Spawn()
	{
		SetModel( "models/vector/square.vmdl" );

		Scale = 3.0f;
		var hull = new BBox( Vector3.Zero, 64f * Scale );
		SetupPhysicsFromOBB( PhysicsMotionType.Keyframed, hull.Mins, hull.Maxs );

		base.Spawn();
	}

	public void CreateBody( int wave )
	{
		Health += wave * WaveHealthScale;
		Health *= DeadLines.PlayerCount();
		SpawnDelay = MathF.Max( 0.1f, SpawnDelay / (wave / 2) );

		var dir = -(Position - Vector3.Zero).Normal;
		DragonBody prevBody = null;
		BBox hull;

		for ( int i = 0; i < BodyParts; i++ )
		{
			var b = new DragonBody();
			b.Position = Position - (dir * i * b.Distance);
			b.Health += wave * WaveHealthScale * 0.5f;
			b.Follow = (i == 0) ? this : prevBody;
			b.Head = this;

			if ( i == 0 ) b.Distance += 16f;

			hull = new BBox( Vector3.Zero, 64f * b.Scale );
			SetupPhysicsFromOBB( PhysicsMotionType.Keyframed, hull.Mins, hull.Maxs );

			prevBody = b;
			Body.Add( b );
		}
	}


	public override void Tick()
	{
		base.Tick();

		if ( !Destroyed )
		{
			if ( NextTarget && ValidTarget() )
			{
				TargetPos = Player.Position.WithZ( 0 );

				// if ( TargetPos.Distance( Position ) <= 400f )
				// {
				// 	NextTarget = 0.5f;
				// 	TargetPos = Rotation.Forward * 2048f;
				// }
			}

			var dir = (TargetPos - Position.WithZ( 0 )).Normal;
			var dot = Vector3.Dot( dir, Rotation.Right );
			Rotation = Rotation.RotateAroundAxis( Vector3.Up, -MathF.Sign( dot ) * TurnSpeed );
			Velocity += (Rotation.Forward * Acceleration) * Time.Delta;

			if ( !DeadLines.Manager.GameOver && NextSpawn )
			{
				// var e = new Arrow();
				// var e = new Square();
				// e.Position = Position + (Rotation.Forward * 32f * Scale);
				// e.Rotation = Rotation;
				// e.Velocity = Velocity;
				// e.Color = Color;
				// e.RenderColor = e.Color;

				var b = new Bullet();
				b.Position = Position + (Rotation.Forward * 32f * Scale);
				b.Rotation = Rotation.From( (TargetPos - Position).EulerAngles );

				NextSpawn = SpawnDelay;
			}
		}

		Velocity -= (Velocity * Drag) * Time.Delta;
		Position = (Position + (Velocity * Time.Delta)).WithZ( 0 );
	}


	public override void OnTouch( Pawn p ) { }
	public override void Knockback( Vector3 vel ) { }

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