using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace DeadLines;


[Category( "Enemies" )]
public partial class Enemy : ModelEntity
{
	public Pawn Player { get; set; }

	[Net]
	public bool Destroyed { get; set; } = false;
	public virtual int AddScore { get; set; } = 2;
	public virtual float BaseHealth { get; set; } = 1f;

	public virtual float Acceleration { get; set; } = 10f;
	public virtual float Drag { get; set; } = 1.0f;

	public virtual Color Color { get; set; } = Color.Red;
	public bool Flashing { get; set; } = false;
	TimeUntil FlashTimer { get; set; } = 0;

	TimeUntil ProxmityCheck;


	public override void Spawn()
	{
		Health = BaseHealth;

		EnableAllCollisions = true;
		EnableTouch = true;
		EnableLagCompensation = true;
		EnableShadowCasting = false;

		RenderColor = Color;

		Tags.Add( "enemy" );
		FindNearestPlayer();
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
		ProxmityCheck = 1.0f;
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
		else if ( Flashing && FlashTimer )
		{
			Flashing = false;
			RenderColor = Color;
		}

		// Switch targets if someone else is closer.
		if ( ProxmityCheck )
			FindNearestPlayer();
	}


	public override void Touch( Entity other )
	{
		base.Touch( other );
		if ( Game.IsClient ) return;

		Log.Info( "StartTouch: " + other );

		if ( other is Pawn p )
			TouchedPlayer( p );
	}


	public virtual void TouchedPlayer( Pawn p )
	{
		if ( Destroyed || p.Dead ) return;

		p.Hurt();

		Destroyed = true;
		Destroy();
	}

	/// <summary>
	/// This enemy has been shot by a hitscan bullet.
	/// </summary>
	public virtual void Shot( TraceResult tr, float damage = 1.0f, float knockback = 20f )
	{
		Health -= damage;
		Knockback( tr.Direction * knockback );

		Flashing = true;
		FlashTimer = 0.2f;
		RenderColor = Color.White;

		if ( Health <= 0 )
		{
			Destroyed = true;
			Destroy();
			DeadLines.ModifyScore( AddScore );
		}
	}

	public virtual void Knockback( Vector3 vel )
	{
		Velocity += vel;
	}

	public virtual void Destroy()
	{
		Destroyed = true; // IMPORTANT: ALWAYS SET THIS
		EnableTraceAndQueries = false;

		RenderColor = Color.Gray.WithAlpha( 100f );
		DeleteAsync( 1.5f );
	}
}
