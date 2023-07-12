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

	public static IReadOnlyDictionary<string, StatDescription> StatDescriptions = GetStatDescriptions();

	public static IReadOnlyDictionary<string, StatDescription> GetStatDescriptions()
	{
		var result = new Dictionary<string, StatDescription>();
		var type = TypeLibrary.GetType( typeof( Pawn ) );
		foreach ( var prop in type.Properties )
		{
			var statDesc = prop.GetCustomAttribute<StatDescription>();
			if ( statDesc != null )
			{
				result.Add( prop.Name, statDesc );
			}
		}
		return result.AsReadOnly();
	}

	[Net, StatDescription( Name = "Max Health", Default = 5, ShopOrder = 1 )]
	public float HealthMax { get; set; }

	[Net, StatDescription( Name = "Move Speed", Default = 700, UpgradeIncrement = 50, ShopOrder = 2 )]
	public float MoveSpeed { get; set; }

	/// <summary>
	/// How many enemies each shot can penetrate.
	/// </summary>
	[Net, StatDescription( Name = "Penetration", ShopOrder = 4 )]
	public int ShotPenetration { get; set; }

	[Net, StatDescription( Name = "Attack Range", Default = 1024, UpgradeIncrement = 64, ShopOrder = 5 )]
	public float ShotDistance { get; set; }
	[Net, StatDescription( Name = "Attack Speed", Default = .25f, Min = 0, UpgradeIncrement = -.03f, ShopOrder = 3 )]
	public float AttackDelay { get; set; }

	[Net, Predicted]
	public TimeUntil AttackCooldown { get; set; }

	[Net]
	public int Bombs { get; set; } = 0;
	[Net, StatDescription( Name = "Max Bombs", Default = 3, ShopOrder = 6 )]
	public int BombsMax { get; set; }

	[Net]
	public bool IsUpgradePanelOpen { get; set; }



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
			ResetUpgrades();
			Components.RemoveAny<PowerupComponent>();
		}

		Dead = false;
		EnableDrawing = true;
		EnableTraceAndQueries = true;

		Health = HealthMax;
		Bombs = BombsMax;

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

		// DEBUG: Slow Motion
		Game.TimeScale = Input.Down( "run" ) ? 0.25f : 1.0f;

		if ( Dead )
			return;

		// Attack
		if ( Input.Down( "attack1" ) )
			TryAttack();

		// Bomb
		if ( Input.Pressed( "bomb" ) )
			DeployBomb();
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

		var endPos = hitCount <= ShotPenetration ? Position + dir * ShotDistance : lastHit.Position;

		_ = new BeamEntity()
		{
			StartPosition = Position + AimRay.Forward * 30f,
			EndPosition = endPos
		};
	}

	public void DeployBomb( bool useAmmo = true )
	{
		if ( !Game.IsServer )
			return;

		if ( useAmmo )
		{
			if ( Bombs <= 0 )
				return;

			Bombs = Math.Max( 0, Bombs - 1 );
		}

		var b = new Bomb();
		b.Position = Position + Vector3.Down;
		b.Explode( 512f, 5f, 1.0f );
	}


	[GameEvent.Client.Frame]
	public void DrawAim()
	{
		if ( Dead ) return;

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

		// TODO: Upgrades for this effect.
		DeployBomb( useAmmo: false );

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
		/*var bullets = 20;
		var angDiff = 360 / 12;

		for ( int i = 0; i < bullets; i++ )
		{
			ShootBullet( Rotation.FromYaw( i * angDiff ).Forward );
		}*/

		var b = new Bomb();
		b.Position = Position + Vector3.Down;
		b.Explode( 256f, 10f, 2.0f );
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

	[ConCmd.Server( "add_upgrade" )]
	public static void AddUpgradeCmd( string propertyName )
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( !pawn.IsValid() )
			return;

		pawn.AddUpgrade( propertyName );
	}

	public void AddUpgrade( string propertyName )
	{
		if ( !Game.IsServer )
			return;

		if ( UpgradePoints <= 0 )
			return;

		var val = TypeLibrary.GetPropertyValue( this, propertyName );
		if ( val == null )
		{
			Log.Error( $"AddUpgrade error: Property {propertyName} not found on Pawn" );
			return;
		}

		var found = StatDescriptions.TryGetValue( propertyName, out var statDesc );
		if ( !found )
		{
			Log.Error( $"AddUpgrade error: StatDescription not found for property {propertyName}" );
			return;
		}

		switch ( val )
		{
			case int tInt:
				tInt += (int)statDesc.UpgradeIncrement;
				val = Math.Max( (int)statDesc.Min, Math.Min( (int)statDesc.Max, tInt ) );
				break;
			case float tFloat:
				tFloat += statDesc.UpgradeIncrement;
				val = Math.Max( statDesc.Min, Math.Min( statDesc.Max, tFloat ) );
				break;
		}

		TypeLibrary.SetProperty( this, propertyName, val );

		Upgrades.TryGetValue( propertyName, out var statUpgradePoints );
		statUpgradePoints++;
		Upgrades[propertyName] = statUpgradePoints;
		UpgradePoints--;
	}

	public void ResetUpgrades()
	{
		Upgrades.Clear();
		foreach ( var pair in StatDescriptions )
		{
			var val = TypeLibrary.GetPropertyValue( this, pair.Key );
			switch ( val )
			{
				case int:
					val = (int)pair.Value.Default;
					break;
				case float:
					val = pair.Value.Default;
					break;
			}
			TypeLibrary.SetProperty( this, pair.Key, val );
		}
	}

	public void ShowUpgradeScreen()
	{
		IsUpgradePanelOpen = true;
	}

	public void HideUpgradeScreen()
	{
		IsUpgradePanelOpen = false;
	}

	[ConCmd.Server]
	public static void HideUpgradeScreenCmd()
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( pawn != null )
			pawn.HideUpgradeScreen();
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

	[Event.Hotload]
	public void Hotload()
	{
		StatDescriptions = GetStatDescriptions();
	}
}
