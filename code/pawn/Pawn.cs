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
	public bool GodMode { get; set; }

	[Net]
	public Item Item { get; set; }


	// 		Upgrades

	[Net]
	public int UpgradePoints { get; set; } = 0;

	[Net]
	public IDictionary<string, int> Upgrades { get; set; }

	[Net]
	public IList<string> AvailableUpgrades { get; set; }

	[Net]
	public IList<string> AvailableItems { get; set; }

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

	[Net, StatDescription( Name = "Max Health", Default = 5, Icon = "favorite" )]
	public float HealthMax { get; set; }

	[Net, StatDescription( Name = "Move Speed", Default = 700, UpgradeIncrement = 70, MaxPoints = 10, Icon = "fast_forward" )]
	public float MoveSpeed { get; set; }

	/// <summary>
	/// How many enemies each shot can penetrate.
	/// </summary>
	[Net, StatDescription( Name = "Penetration", MaxPoints = 5, Icon = "keyboard_tab" )]
	public int ShotPenetration { get; set; }

	[Net, StatDescription( Name = "Attack Range", Default = 1024, UpgradeIncrement = 96, MaxPoints = 5, Icon = "east" )]
	public float ShotDistance { get; set; }
	[Net, StatDescription( Name = "Attack Speed", Default = .25f, Min = .04f, UpgradeIncrement = -.03f, Icon = "autorenew" )]
	public float AttackDelay { get; set; }

	[Net, Predicted]
	public TimeUntil AttackCooldown { get; set; }

	[Net]
	public int Bombs { get; set; } = 0;
	[Net, StatDescription( Name = "Max Bombs", Default = 3, Icon = "brightness_1" )]
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
			// DEBUG: Give Item
			// Item = new ItemTimeWatch();
			// Item = new ItemForceField();
			// Item = new ItemHealthKit();
			// Item = new ItemSuperBomb();

			ResetUpgrades();
			Components.RemoveAny<PowerupComponent>();
		}
		else
		{
			foreach ( var comp in Components.GetAll<PowerupComponent>() )
			{
				comp.Toggle( true );
			}
		}

		Dead = false;
		EnableDrawing = true;
		EnableTraceAndQueries = true;

		Health = HealthMax;
		Bombs = BombsMax;
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
		// Game.TimeScale = Input.Down( "run" ) ? 0.25f : 1.0f;

		if ( Dead )
			return;

		// Attack
		if ( Input.Down( "attack1" ) )
			TryAttack();

		// Bomb
		if ( Input.Pressed( "bomb" ) )
			DeployBomb();

		// Item
		if ( Input.Pressed( "item" ) )
			UseItem();
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
		Vector3 endPos = Position + (dir * ShotDistance);
		Vector3 lastHitPos = endPos;

		foreach ( var tr in hits )
		{
			if ( tr.Entity is Enemy e && CanHit( e ) )
			{
				e.Shot( tr );
				lastHitPos = tr.HitPosition;

				hitCount++;
			}

			// Penetration Counter
			if ( hitCount > ShotPenetration )
			{
				// Log.Info( "Shot " + hitCount + " enemies at once." );
				break;
			}
		}

		endPos = hitCount <= ShotPenetration ? endPos : lastHitPos;

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
		b.Explode( 512f, 5f, 2.0f );

		Sound.FromEntity( To.Everyone, "player.bomb", this );
	}

	public void UseItem()
	{
		if ( !Game.IsServer )
			return;

		Item?.OnUse( this );
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
		if ( GodMode )
			return;

		Explode();
		Health -= damage;

		if ( Health <= 0 )
			Die();
	}

	public void Die()
	{
		DeadLines.PlayerDied( this.Client );

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
			.WithAnyTags( "enemy" );
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

	[ConCmd.Server]
	public static void AddPawnUpgradeCmd( string propertyName )
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( !pawn.IsValid() || !pawn.IsUpgradePanelOpen || !pawn.AvailableUpgrades.Contains( propertyName ) )
			return;

		pawn.AddUpgrade( propertyName );
	}

	public void AddUpgrade( string propertyName )
	{
		if ( !Game.IsServer )
			return;

		if ( UpgradePoints <= 0 )
			return;

		if ( !propertyName.StartsWith( "Powerup-" ) )
		{
			this.IncrementStat( propertyName );
			if ( IsUpgradePanelOpen ) // Prevent cheaters from using AddUpgradeCmd to full heal
			{
				if ( propertyName == "HealthMax" )
					Health = HealthMax;
				if ( propertyName == "BombsMax" )
					Bombs = BombsMax;
			}
		}
		else
		{
			var type = propertyName.Substring( "Powerup-".Length );
			var comps = Components.GetAll<PowerupComponent>();
			if ( !comps.Any( x => x.GetType().ToString() == type ) )
			{
				var comp = TypeLibrary.Create( type, typeof( PowerupComponent ) );
				Components.Add( (PowerupComponent)comp );
				AvailableUpgrades.Remove( propertyName );
			}
		}

		Upgrades.TryGetValue( propertyName, out var statUpgradePoints );
		statUpgradePoints++;
		Upgrades[propertyName] = statUpgradePoints;
		UpgradePoints--;

		StatDescriptions.TryGetValue( propertyName, out var desc );
		if ( desc != null && statUpgradePoints >= desc.MaxPoints && AvailableUpgrades.Contains( propertyName ) )
			AvailableUpgrades.Remove( propertyName );
	}

	[ConCmd.Server]
	public static void BuyItemCmd( string typeName )
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( !pawn.IsValid() || !pawn.IsUpgradePanelOpen || !pawn.AvailableItems.Contains( typeName ) )
			return;

		pawn.BuyItem( typeName );
	}

	public void BuyItem( string typeName )
	{
		if ( !Game.IsServer )
			return;

		Item = (Item)TypeLibrary.Create( typeName, typeof( Item ) );
	}

	public void ResetUpgrades()
	{
		UpgradePoints = 0;
		Upgrades.Clear();
		this.ResetStats();
	}

	public void ShowUpgradeScreen()
	{
		RandomizeAvailableUpgrades();
		RandomizeAvailableItems();
		IsUpgradePanelOpen = true;
	}

	public void RandomizeAvailableUpgrades()
	{
		AvailableUpgrades.Clear();

		var available = this.GetUpgradeableStatPropNames();

		var attachedPowerups = Components.GetAll<PowerupComponent>();
		var absentPowerups = new List<(string PropName, IList<string> AvailableList)>();
		foreach ( var p in TypeLibrary.GetTypes<PowerupComponent>() )
		{
			if ( p.Name == "PowerupComponent" )
				continue;

			if ( attachedPowerups.FirstOrDefault( x => x.GetType() == p.TargetType ) != null )
				continue;

			absentPowerups.Add( new( "Powerup-" + p.FullName, AvailableUpgrades ) );
		}

		var powerupUpgrades = new List<(string PropName, IList<string> AvailableList)>();
		foreach ( var powerup in attachedPowerups )
		{
			powerup.AvailableUpgrades.Clear();
			powerupUpgrades.AddRange( powerup.GetUpgradeableStatPropNames() );
		}

		available.Shuffle();
		powerupUpgrades.Shuffle();
		absentPowerups.Shuffle();

		for ( var i = 0; i < 3; i++ )
		{
			(string PropName, IList<string> AvailableList)? itemToAdd = null;
			var rand = Game.Random.Next( 5 );
			if ( rand < 4 )
				itemToAdd = available.PopStruct();

			if ( itemToAdd == null || rand == 4 )
			{
				rand = Game.Random.Next( 3 );
				if ( rand < 2 )
					itemToAdd = powerupUpgrades.PopStruct();

				if ( itemToAdd == null || rand == 2 )
					itemToAdd = absentPowerups.PopStruct();
			}

			if ( itemToAdd == null )
				itemToAdd = available.PopStruct() ?? powerupUpgrades.PopStruct();

			if ( itemToAdd != null )
				itemToAdd.Value.AvailableList.Add( itemToAdd.Value.PropName );
		}
	}

	public void RandomizeAvailableItems()
	{
		AvailableItems.Clear();

		var types = TypeLibrary.GetTypes<Item>();
		if ( Item != null )
			types = types.Where( x => x.TargetType != Item.GetType() && x.TargetType != typeof( Item ) );


		var items = types.Select( x => x.TargetType.FullName ).ToList();
		items.Shuffle();

		for ( var i = 0; i < 2; i++ )
		{
			var item = items.Pop();
			if ( !string.IsNullOrEmpty( item ) )
				AvailableItems.Add( item );
		}
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
