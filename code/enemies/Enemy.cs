using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace DeadLines;


[Category( "Enemies" )]
public partial class Enemy : ModelEntity
{
	/// <summary>
	/// Active player target.
	/// </summary>
	public Pawn Player { get; set; }
	public Vector3 TargetPos { get; set; } = Vector3.Zero;

	/// <summary>
	/// Automatically detect the nearest player.
	/// </summary>
	public virtual bool AutoDetect { get; set; } = true;

	[Net]
	public bool Destroyed { get; set; } = false;

	public virtual int AddScore { get; set; } = 2;
	public virtual float BaseHealth { get; set; } = 1f;

	public virtual float Acceleration { get; set; } = 10f;
	public virtual float Drag { get; set; } = 1.0f;

	public virtual Color Color { get; set; } = Color.Red;

	public virtual string HitSound { get; set; } = "";
	public bool HitFlash { get; set; } = false;
	TimeUntil FlashTimer { get; set; } = 0;

	TimeUntil ProximityCheck;


	public override void Spawn()
	{
		Health = BaseHealth;

		EnableTraceAndQueries = true;
		EnableAllCollisions = true;
		EnableLagCompensation = true;
		EnableShadowCasting = false;

		RenderColor = Color;

		Tags.Add( "enemy" );
	}


	public IOrderedEnumerable<Pawn> GetPlayersByDistance()
	{
		List<Pawn> pawns = new();

		foreach ( var ent in Entity.All )
		{
			if ( ent is Pawn p && !p.Dead )
				pawns.Add( p );
		}

		return pawns.OrderBy( p => p.Position.DistanceSquared( Position ) );
	}

	public void FindNearestPlayer()
	{
		// TODO: Distance sorting.
		Player = GetPlayersByDistance().FirstOrDefault();
		ProximityCheck = 1.0f;
	}

	public bool ValidTarget()
	{
		if ( Player == null )
			FindNearestPlayer();

		return Player.IsValid();
	}

	public TraceResult PlayerTrace()
	{
		return Trace.Sphere( 32f, Position, Position )
			.WithTag( "player" )
			.Run();
	}

	public Pawn TouchingPlayer()
	{
		if ( Destroyed ) return null;

		var tr = PlayerTrace();
		if ( !tr.Hit || !tr.Entity.IsValid() ) return null;

		return tr.Entity as Pawn;
	}


	[GameEvent.Tick.Server]
	public virtual void Tick()
	{
		if ( Destroyed )
		{
			RenderColor = RenderColor.WithAlpha( RenderColor.a - Time.Delta );
			if ( RenderColor.a == 0 ) Delete();
		}
		else if ( HitFlash && FlashTimer )
		{
			HitFlash = false;
			RenderColor = Color;
		}

		// Switch targets if someone else is closer.
		if ( AutoDetect && ProximityCheck )
			FindNearestPlayer();
	}


	public override void Touch( Entity other )
	{
		base.Touch( other );
		if ( Game.IsClient ) return;

		// Log.Info( "StartTouch: " + other );

		if ( other is Pawn p )
			TouchedPlayer( p );
	}


	public virtual void TouchedPlayer( Pawn p )
	{
		if ( Destroyed || p.Dead ) return;

		p.Hurt();
		OnTouch( p );
	}

	public virtual void OnTouch( Pawn p )
	{
		Destroyed = true;
		Destroy();
	}

	/// <summary>
	/// This enemy has been shot by a hitscan bullet.
	/// </summary>
	public virtual void Shot( TraceResult tr, float dmg = 1.0f, float knockback = 500f )
	{
		Hurt( dmg );
		Knockback( tr.Direction * knockback );
	}

	public virtual void Hurt( float dmg )
	{
		Health -= dmg;

		HitFlash = true;
		FlashTimer = 0.2f;
		RenderColor = Color.White;

		if ( HitSound != "" )
			Sound.FromEntity( To.Everyone, HitSound, this );

		if ( Health <= 0 )
		{
			Destroyed = true;
			Destroy( cleanup: false );
			DeadLines.ModifyScore( AddScore );
		}
	}

	public virtual void Knockback( Vector3 vel )
	{
		Velocity += vel;
	}

	public virtual void Destroy( bool cleanup = false )
	{
		Destroyed = true; // IMPORTANT: ALWAYS SET THIS
		EnableTraceAndQueries = false;

		RenderColor = Color.Gray.WithAlpha( MathF.Min( 100f, RenderColor.a ) );
		DeleteAsync( 1.5f );
	}
}
