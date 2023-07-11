using System;
using Sandbox;

namespace DeadLines;


public partial class ChainBall : ModelEntity
{

	[Net]
	public Pawn Player { get; set; }
	public bool Disconnected { get; set; }

	public float Rigidity { get; set; } = 2f;
	public float PullSpeed { get; set; } = 15f;
	public float HitForce { get; set; } = 2.0f;
	public float ChainLength { get; set; } = 128.0f;
	public float Drag { get; set; } = 1.0f;

	public float DefaultScale { get; set; } = 2.0f;
	public Color Color { get; set; } = Color.Magenta;


	public override void Spawn()
	{
		SetModel( "models/vector/circle.vmdl" );
		Scale = DefaultScale;
		RenderColor = Color;

		SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 32f );

		EnableTraceAndQueries = true;
		EnableAllCollisions = true;
		EnableShadowCasting = false;

		Tags.Add( "ball" );
	}


	public override void Touch( Entity other )
	{
		base.Touch( other );

		if ( !Game.IsServer )
			return;

		if ( other is Enemy e )
		{
			e.Velocity += Velocity * HitForce * Time.Delta;
			// e.Velocity = Velocity;
		}
	}


	[GameEvent.Tick.Server]
	public void Tick()
	{
		if ( Player.IsValid() && !Player.Dead )
		{
			var pos = Player.Position;
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
			Player = null;
			Disconnected = true;

			RenderColor = Color.Gray.WithAlpha( RenderColor.a - Time.Delta );

			if ( RenderColor.a <= 0 )
				Delete();
		}

		// Velocity -= (Velocity * Drag) * Time.Delta;
		Position += Velocity * Time.Delta;
	}

	[GameEvent.Client.Frame]
	public void DrawChain()
	{
		if ( Player.IsValid() && !Player.Dead )
			DebugOverlay.Line( Position, Player.Position, Color.Gray.WithAlpha( 50f ), 0.00f, true );
	}
}