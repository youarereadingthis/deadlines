using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeadLines;


public partial class Pawn : AnimatedEntity
{

	[ClientInput]
	public Vector3 InputDirection { get; set; }

	[ClientInput]
	public Angles AimAngles { get; set; }
	public override Ray AimRay => new( Position + Vector3.Up * 14f, AimAngles.Forward );

	[BindComponent] public PawnController Controller { get; }


	[Net]
	public bool Dead { get; set; } = false;

	[Net]
	public int Lives { get; set; } = 1;


	// 		Upgrades

	[Net]
	public float MaxHealth { get; set; }
	[ConVar.Replicated( "dl_maxhp" )]
	public static int MaxHealthDefault { get; set; } = 5;

	[Net]
	public float MoveSpeed { get; set; }
	public float MoveSpeedDefault { get; set; } = 400f;

	/// <summary>
	/// How many enemies each shot can penetrate.
	/// </summary>
	[Net]
	public int ShotPenetration { get; set; } = 0;

	[Net]
	public float ShotDistance { get; set; }
	public float ShotDistanceDefault { get; set; } = 1024f;
	[Net]
	public float AttackDelay { get; set; } = .5f;

	[Net]
	public TimeUntil AttackCooldown { get; set; }




	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		SetModel( "models/vector/triangle.vmdl" );
		// SetupPhysicsFromModel( PhysicsMotionType.Static ); // needs "hullfromrender" in modeldoc
		SetupPhysicsFromSphere( PhysicsMotionType.Static, Vector3.Zero, 24f );

		EnableTouch = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowCasting = false;

		Tags.Add( "player" );
	}

	public void Respawn( bool resetStats = true )
	{
		Components.Create<PawnController>();

		// Might respawn during coop. Don't reset their stats in that case.
		if ( resetStats )
		{
			Lives = 1;
			MaxHealth = MaxHealthDefault;
			MoveSpeed = MoveSpeedDefault;

			ShotPenetration = 0;
			ShotDistance = ShotDistanceDefault;
		}

		Dead = false;
		EnableDrawing = true;
		EnableTraceAndQueries = true;

		Health = MaxHealth;
	}

	public override void Simulate( IClient cl )
	{
		SimulateRotation();
		Controller?.Simulate( cl );

		// DEBUG: Text Overlay
		var textPos = new Vector2( 20, 20 );
		var textLife = 0.04f;
		DebugOverlay.ScreenText( "Score: " + DeadLines.Manager.Score, textPos, 1, Color.White, textLife );
		DebugOverlay.ScreenText( "Wave: " + DeadLines.Manager.WaveCount, textPos, 2, Color.White, textLife );
		DebugOverlay.ScreenText( "Health: " + Health, textPos, 3, Color.Orange, textLife );

		if ( Dead )
		{
			if ( Game.IsServer )
			{
				if ( Input.Pressed( "reload" ) )
					DeadLines.RequestRestart();
			}
			else if ( Game.IsClient && DeadLines.Manager.GameOver )
			{
				DrawGameOver();
			}

			return;
		}
		// Game.TimeScale = Input.Down( "run" ) ? 0.25f : 1.0f;

		// TODO: Proper Aim Laser & Cursor
		DrawAim();

		// Attack
		if ( Input.Down( "attack1" ) )
			TryAttack();
	}

	private void DrawGameOver()
	{
		var textLife = 0.04f;
		var textPos = Screen.Size / 2;
		DebugOverlay.ScreenText( "GAME OVER", textPos, 1, Color.Red, textLife );
		DebugOverlay.ScreenText( "press RELOAD to restart", textPos, 2, Color.White, textLife );
	}


	public void TryAttack()
	{
		if ( !AttackCooldown )
			return;

		ShootBullet( AimRay.Forward );
		AttackCooldown = AttackDelay;
	}

	public void ShootBullet( Vector3 dir )
	{
		// TODO: Client Shoot Effects

		if ( !Game.IsServer ) return;

		TraceResult[] hits;
		var trace = AimTrace( Position, dir );

		// Lagcomp for Hitscan
		using ( LagCompensation() )
		{
			hits = RunBulletTrace( trace ) ?? Array.Empty<TraceResult>();
		}

		var hitCount = 0;
		Enemy lastHit = null;

		foreach ( var tr in hits )
		{
			if ( tr.Entity is Enemy e && CanHit( e ) )
			{
				e.Shot( tr );
				lastHit = e;
				hitCount++;
			}

			// Penetration Counter
			if ( hitCount > ShotPenetration )
			{
				// Log.Info( "Shot " + hitCount + " enemies at once." );
				break;
			}
		}


		var endPos = lastHit?.Position ?? Position + AimRay.Forward * ShotDistance;

		_ = new BeamEntity()
		{
			StartPosition = Position + AimRay.Forward * 30f,
			EndPosition = endPos
		};
	}

	public void DrawAim()
	{
		if ( !Game.IsClient ) return;

		// TODO: Shot delay.
		var hits = RunBulletTrace( AimTrace() );
		if ( hits == null ) return;

		foreach ( var tr in hits )
		{
			if ( tr.Entity is Enemy e && CanHit( e ) )
			{
				DebugOverlay.Line( tr.StartPosition, tr.HitPosition, Color.Gray, Time.Delta );
				DebugOverlay.Circle( tr.HitPosition, Rotation.FromPitch( 90f ), 4f, Color.White, Time.Delta );

				return;
			}
		}
	}




	public void Hurt( float damage = 1f )
	{
		Explode();
		Health -= damage;

		if ( Health <= 0 )
			Die();
	}

	public void Die()
	{
		// TODO: Play sound.

		Explode();

		EnableDrawing = false;
		EnableTraceAndQueries = false;

		// Subtract a life.
		Lives--;

		if ( Lives <= 0 )
		{
			Dead = true;

			if ( DeadLines.AllDead() )
				DeadLines.GameEnd();
		}
	}

	/// <summary>
	/// Explode on death/hurt, pushing enemies back and dealing damage.
	/// </summary>
	public void Explode()
	{
		// Burst of Bullets
		var bullets = 20;
		var angDiff = 360 / 12;

		for ( int i = 0; i < bullets; i++ )
		{
			ShootBullet( Rotation.FromYaw( i * angDiff ).Forward );
		}
	}


	public static bool CanHit( Enemy e )
	{
		return !e.Destroyed;
	}

	public Trace AimTrace()
	{
		var aimRay = AimRay;
		return AimTrace( Position, aimRay.Forward );
	}

	public Trace AimTrace( Vector3 startPos, Vector3 dir, float? distance = null )
	{
		var dist = distance ?? ShotDistance;
		return Trace.Ray( startPos, startPos + (dir * dist) )
			.WithAnyTags( "enemy", "ball" );
	}

	/// <summary>
	/// Trace a pentrating shot that is ordered by distance.
	/// </summary>
	public static TraceResult[] RunBulletTrace( Trace trace )
	{
		var hits = trace.RunAll();
		if ( hits == null ) return hits;

		return hits.OrderBy( tr => tr.Distance ).ToArray();
	}


	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;

		AimAngles = (DeadLines.MouseWorldPos() - Position).WithZ( 0 ).EulerAngles;
	}

	public override void FrameSimulate( IClient cl )
	{
		SimulateRotation();
	}

	protected void SimulateRotation()
	{
		Rotation = AimAngles.ToRotation();
	}
}
