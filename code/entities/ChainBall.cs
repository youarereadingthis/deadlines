using System;
using Sandbox;

namespace DeadLines;


public partial class ChainBall : ModelEntity
{

	[Net]
	public Entity Follow { get; set; }
	public float Rigidity { get; set; } = 2.8f;
	public float PullSpeed { get; set; } = 15f;
	public float HitForce { get; set; } = 50.0f;
	public float ChainLength { get; set; } = 128.0f;
	public float Drag { get; set; } = 1.0f;

	public static float DefaultScale = 2.0f;
	public Color Color { get; set; } = Color.Magenta;

	[Net]
	private bool _active { get; set; } = true;
	public bool Active
	{
		get
		{
			return _active;
		}
		set
		{
			EnableAllCollisions = value;
			RenderColor = Color.White;
			_active = value;
		}
	}

	private float _ballScale = DefaultScale;
	public float BallScale
	{
		get
		{
			return _ballScale;
		}
		set
		{
			_ballScale = value;
			Scale = _ballScale;
			SetupCollisionSphere();
		}
	}


	public override void Spawn()
	{
		SetModel( "models/vector/circle.vmdl" );
		Scale = BallScale;
		RenderColor = Color;

		SetupCollisionSphere();

		EnableAllCollisions = true;
		EnableShadowCasting = false;

		Tags.Add( "ball" );
	}

	public void SetupCollisionSphere()
	{
		SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 16 * DefaultScale );
	}

	public override void Touch( Entity other )
	{
		base.Touch( other );

		if ( !Game.IsServer )
			return;

		if ( other is Enemy e )
		{
			e.Velocity += Velocity * HitForce * Time.Delta * Scale;
			// e.Velocity = Velocity;
		}
	}


	[GameEvent.Tick.Server]
	public void Tick()
	{
		if ( Active && Follow.IsValid() )
		{
			var pos = Follow.Position;
			var dist = Position.Distance( pos );

			if ( dist > ChainLength )
			{
				var slack = dist - ChainLength;
				var dir = (pos - Position).Normal;

				Velocity += dir * PullSpeed * slack * Time.Delta;

				// Pull us in if we've exited the sphere.
				if ( slack > 0 )
					Position -= -dir * slack * Rigidity * Time.Delta;
			}

			// TODO: Fix these calculations for 2D.
			/*var origin = Player.Position;
			var dist = Position.Distance( origin );

			if ( dist >= ChainLength )
			{
				var slack = dist - ChainLength;
				var line = origin - Position;
				var dir = line.Normal;

				// Encircle the sphere.
				// if ( Velocity.Dot( dir ) >= 0 )
					Velocity -= Velocity.ProjectOnNormal( -dir ).Normal * slack * 1f;

				// Pull us in if we've exited the sphere.
				if ( slack > 0 )
					Position -= -dir * slack * Time.Delta;
			}*/
		}
		else
		{
			RenderColor = Color.Gray.WithAlpha( Math.Max( 0, RenderColor.a - Time.Delta ) );
			Velocity *= .92f;

			if ( !Follow.IsValid() && RenderColor.a <= 0 )
				Delete();
		}

		// Velocity -= (Velocity * Drag) * Time.Delta;
		Position = (Position + (Velocity * Time.Delta)).WithZ( 0 );
	}

	[GameEvent.Client.Frame]
	public void DrawChain()
	{
		if ( Active && Follow.IsValid() )
			DebugOverlay.Line( Position, Follow.Position, Color.Gray.WithAlpha( 50f ), 0.00f, true );
	}
}
