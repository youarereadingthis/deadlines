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


	public override void Spawn()
	{
		Health = BaseHealth;

		EnableAllCollisions = true;
		EnableTouch = true;
		EnableLagCompensation = true;
		EnableShadowCasting = false;

		Tags.Add( "enemy" );
	}


	public void FindNearestPlayer()
	{
		// TODO: Distance sorting.

		foreach ( var ent in Entity.All )
		{
			if ( ent is Pawn p )
				Player = p;
		}
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
		var tr = PlayerTrace();
		if ( !tr.Hit || !tr.Entity.IsValid() ) return null;

		return tr.Entity as Pawn;
	}


	[GameEvent.Tick.Server]
	public virtual void Think()
	{
		if ( Destroyed )
		{
			RenderColor = RenderColor.WithAlpha( RenderColor.a - Time.Delta );
			if ( RenderColor.a == 0 ) Delete();
		}
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
		RenderColor = Color.Gray.WithAlpha( 100f );
		DeleteAsync( 1.5f );
	}
}