using System;
using System.ComponentModel;
using Sandbox;

namespace DeadLines;


public partial class Bomb : ModelEntity
{
	[Net]
	public TimeUntil ShouldDelete { get; set; }
	[Net]
	public float Duration { get; set; } = 2f;
	[Net]
	public float Radius { get; set; } = 512f;
	public float PushForce { get; set; } = 10f;

	public Color Color { get; set; } = Color.Gray;


	public override void Spawn()
	{
		SetModel( "models/vector/circle.vmdl" );

		EnableDrawing = false;
		EnableTraceAndQueries = true;
		EnableAllCollisions = true;
		EnableTouchPersists = true;
	}


	public void Explode( float radius, float dmg, float duration = 2f )
	{
		Duration = duration;
		ShouldDelete = duration;

		Radius = radius;
		SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, Radius );

		// Do damage immediately upon explosion.
		foreach ( Entity ent in Entity.FindInSphere( Position, Radius ) )
		{
			if ( ent is Enemy e )
			{
				e.Hurt( dmg );
			}
		}
	}

	public override void Touch( Entity other )
	{
		if ( !Game.IsServer ) return;

		// Push enemies away.
		if ( other is Enemy e )
		{
			var dir = (e.Position - Position).Normal;
			e.Velocity += dir * PushForce;
		}
	}


	[GameEvent.Tick.Server]
	public void Tick()
	{
		if ( ShouldDelete )
		{
			Delete();
			return;
		}

		// Scale = MathX.Lerp( 0.3f, 1.0f, ShouldDelete.Fraction, true );
	}

	[GameEvent.Client.Frame]
	public void DrawRadius()
	{
		var frac = ShouldDelete.Fraction;
		Color = Color.WithAlpha( 1f - frac );

		frac = MathF.Min( frac * 5f, 1f );
		DebugOverlay.Sphere( Position, Radius * frac, Color, 0f, false );
	}
}