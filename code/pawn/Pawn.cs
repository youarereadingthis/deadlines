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
	public int UpgradePoints { get; set; } = 0;

	[Net]
	public IDictionary<string, int> Upgrades { get; set; }

	[Net]
	public float MaxHealth { get; set; }
	public static int MaxHealthDefault { get; set; } = 5;

	[Net]
	public float MoveSpeed { get; set; }
	public float MoveSpeedDefault { get; set; } = 700f;

	/// <summary>
	/// How many enemies each shot can penetrate.
	/// </summary>
	[Net]
	public int ShotPenetration { get; set; } = 0;

	[Net]
	public float ShotDistance { get; set; }
	public float ShotDistanceDefault { get; set; } = 1024f;
	[Net]
	public float AttackDelay { get; set; } = .25f;

	[Net, Predicted]
	public TimeUntil AttackCooldown { get; set; }




	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		SetModel( "models/vector/triangle.vmdl" );
		// SetupPhysicsFromModel( PhysicsMotionType.Static ); // needs "hullfromrender" in modeldoc
		SetupPhysicsFromSphere( PhysicsMotionType.Static, Vector3.Backward * 8f, 24f );

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
			UpgradePoints = 0;
			Upgrades.Clear();

			MaxHealth = MaxHealthDefault;
			MoveSpeed = MoveSpeedDefault;

			ShotPenetration = 0;
			ShotDistance = ShotDistanceDefault;

			Components.RemoveAny<PowerupComponent>();
		}

		Dead = false;
		EnableDrawing = true;
		EnableTraceAndQueries = true;

		Health = MaxHealth;

		// DEBUG: Spawn with Ball
		Components.GetOrCreate<ChainBallComponent>();
	}

	public override void Simulate( IClient cl )
	{
		SimulateRotation();
		Controller?.Simulate( cl );

		if ( DeadLines.Manager.GameOver )
		{
			if ( Game.IsServer )
				if ( Input.Pressed( "reload" ) )
					DeadLines.RequestRestart();
		}

		// Game.TimeScale = Input.Down( "run" ) ? 0.25f : 1.0f;

		if ( Dead )
			return;

		// Attack
		if ( Input.Down( "attack1" ) )
			TryAttack();
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
		Sound.FromEntity( To.Everyone, "pow1", this );

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

	[GameEvent.Client.Frame]
	public void DrawAim()
	{
		if ( Dead ) return;

		// TODO: Shot delay.
		var hits = RunBulletTrace( AimTrace() );
		if ( hits == null ) return;

		foreach ( var tr in hits )
		{
			if ( tr.Entity is Enemy e && CanHit( e ) )
			{
				DebugOverlay.Line( tr.StartPosition, tr.HitPosition, Color.Gray, 0f );
				DebugOverlay.Circle( tr.HitPosition, Rotation.FromPitch( 90f ), 4f, Color.White, 0f );

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

		Dead = true;

		foreach ( var comp in Components.GetAll<PowerupComponent>() )
		{
			comp.Toggle( false );
		}

		if ( DeadLines.AllDead() )
			DeadLines.GameEnd();
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


	public void AddUpgrade( string name )
	{
		// int level = Upgrades.GetOrCreate( name );
		Upgrades[name]++;
	}

	public void ResetUpgrades()
	{
		// TODO: Delegates
		Upgrades.Clear();
	}

	public void ShowUpgradeScreen()
	{

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
