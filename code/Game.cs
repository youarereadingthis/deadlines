
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Sandbox;
using Sandbox.Physics;
using Sandbox.Services;

namespace DeadLines;


/// <summary>
/// Game Manager.
/// </summary>
public partial class DeadLines : Sandbox.GameManager
{
	[Net]
	public int Score { get; set; } = 0;
	[Net]
	public bool CoopMode { get; set; } = false;
	[Net]
	public bool GameOver { get; set; } = true;
	[Net]
	public float ArenaSize { get; set; } = 2048f;


	public static DeadLines Manager => DeadLines.Current as DeadLines;


	public static List<Pawn> GetPlayers()
	{
		List<Pawn> pawns = new();

		foreach ( var cl in Game.Clients )
		{
			if ( cl.Pawn is Pawn p )
				pawns.Add( p );
		}

		return pawns;
	}

	public static uint PlayerCount()
	{
		uint count = 0;

		foreach ( var cl in Game.Clients )
			if ( cl.Pawn.IsValid() )
				count++;

		return count;
	}

	public static uint EnemyCount()
	{
		uint count = 0;

		foreach ( Entity ent in Entity.All )
			if ( ent is Enemy e )
				count++;

		return count;
	}

	public static bool AllDead()
	{
		foreach ( var cl in Game.Clients )
			if ( cl.Pawn is Pawn p && !p.Dead )
				return false;

		return true;
	}

	public override void ClientSpawn()
	{
		Camera.Main.AmbientLightColor = Color.White;
		Game.RootPanel = new Hud();
	}


	public static void ModifyScore( int score )
	{
		Manager.Score += score;
	}

	public static void RequestRestart()
	{
		if ( !Manager.GameOver )
			return;

		Restart();
	}

	public static void Restart()
	{
		Game.TimeScale = 1.0f;
		Manager.Score = 0;
		Manager.WaveCount = 0;
		Manager.GameOver = false;

		StartWave( 0f );
		StartBursting();

		foreach ( Entity ent in Entity.All )
		{
			if ( ent is Enemy e )
			{
				e.Destroy();
			}
		}

		foreach ( Pawn p in GetPlayers() )
		{
			p.Respawn( resetStats: true );
			// p.Position = Vector3.Zero;
		}
	}

	public static void GameEnd()
	{
		Game.TimeScale = 0.5f;
		Manager.GameOver = true;

		SubmitScores( Manager.Score );
	}

	/// <summary>
	/// End the game if there are no pawns left.
	/// </summary>
	public void AllDeadCheck()
	{
		if ( !GameOver && AllDead() )
			GameEnd();
	}

	[ClientRpc]
	public static void SubmitScores( int score )
	{
		Log.Info( "Submitted Score: " + score );
		Stats.SetValue( "highscore", score );
	}

	[ClientRpc]
	public static void PlayerDied( IClient cl )
	{
		if ( cl == Game.LocalClient )
			Stats.Increment( "deaths", 1 );
	}


	[Net]
	public int WaveCount { get; set; } = 0;
	[Net]
	public TimeUntil NextWave { get; set; } = 0f;
	public static float SpawnBank { get; set; } = 100f;
	public static float SpawnBankMax { get; set; } = 100f;

	// Level of challenge, per wave.
	public static float IntensityMin { get; set; } = 0f;
	public static float IntensityMax { get; set; } = 200f;
	public static float IntensityLimit { get; set; } = 500f;
	public static float BaseIntensity { get; set; } = 100f;
	public static float MostIntenseWave { get; set; } = 15f;

	// Global limit of spawn rates.
	public static float SpawnDelayMin { get; set; } = 0.1f;
	public static float SpawnDelayMax { get; set; } = 1.5f;
	public static TimeUntil NextSpawn { get; set; } = 0f;

	// Sometimes spawn bursts of enemies.
	public static TimeUntil NextBurst { get; set; } = 0f;
	public static float BurstDelayMin { get; set; } = 50.0f;
	public static float BurstDelayMax { get; set; } = 90.0f;
	public static bool SpawningBurst { get; set; } = false;


	public enum WaveType
	{
		Continuous, // Just spawn enemies on a delay until we can't anymore.
		Burst, // Spawn sudden bursts of enemies on a delay.
	}


	[GameEvent.Tick.Server]
	public void Tick()
	{
		WaveLogic();
		AllDeadCheck();
	}

	public void WaveLogic()
	{
		if ( GameOver )
			return;

		if ( NextWave )
		{
			foreach ( Pawn p in GetPlayers() )
			{
				p.HideUpgradeScreen();
			}

			// Continuously spawn until we're outta juice.
			// Then wait until the next wave.

			if ( SpawnBank <= 0f )
			{
				if ( EnemyCount() == 0 )
					FinishWave();
			}
			else if ( NextBurst && SpawnBank > 100f )
			{
				StartBursting();
			}
			else if ( NextSpawn )
			{
				if ( SpawningBurst )
				{
					// Give them some time to deal with burst.
					NextSpawn = 10f;
					SpawningBurst = false;

					// Select a random enemy to spawn a lot of.
					SpawnEnemyBurst();
				}
				else
				{
					// How far is the wave along?
					float skill = MathX.Lerp( IntensityMin, IntensityMax, 1f - (SpawnBank / SpawnBankMax), true );
					float frac = MathX.Clamp( skill / IntensityLimit, 0f, 1f );
					// Log.Info( "skill:" + skill );
					// Log.Info( "frac:" + frac );

					// Spawn faster as the wave proceeds.
					var pCount = MathF.Max( 1f, PlayerCount() );
					NextSpawn = MathX.Lerp( SpawnDelayMax, SpawnDelayMin, frac, true ) * pCount;
					// Log.Info( "NextSpawn:" + NextSpawn );
					// Spawn the enemy and subtract its value from our bank.
					SpawnBank = MathF.Max( 0f, SpawnBank - SpawnEnemy() );
				}
			}
		}
	}

	public static void FinishWave()
	{
		if ( !Manager.GameOver )
		{
			// Respawn dead players. Heal living ones.
			foreach ( Pawn p in GetPlayers() )
			{
				p.UpgradePoints++;
				p.ShowUpgradeScreen();
				Sound.FromEntity( To.Single( p ), "player.levelup", p );

				if ( p.Dead )
					p.Respawn( resetStats: false );
				else
				{
					p.Health = p.HealthMax;
					p.Bombs = p.BombsMax;
					// p.Bombs = Math.Min( p.Bombs + 1, p.BombsMax );
				}
			}
		}

		Log.Info( "You survived wave " + Manager.WaveCount + "!" );
		StartWave( 10f );
	}

	public static void StartWave( float delay = 10f )
	{
		// Spawn more enemies per wave.
		var pCount = MathF.Max( 1f, PlayerCount() );
		SpawnBankMax = (500f + (Manager.WaveCount * 50f)) * pCount;
		SpawnBank = SpawnBankMax;

		// Reach max intensity at a certain level.
		// From then on, only the minimum intensity may increase.
		var frac = Manager.WaveCount / MostIntenseWave;
		IntensityMin = MathF.Min( IntensityLimit, (IntensityLimit * frac) * 0.4f );
		IntensityMax = MathF.Min( IntensityLimit, BaseIntensity + (IntensityLimit * frac) );

		// Log.Info( "IntensityMin:" + IntensityMin );
		// Log.Info( "IntensityMax:" + IntensityMax );

		DeadLines.Manager.NextWave = delay;
		NextSpawn = 0f;

		Manager.WaveCount++;
	}

	public static void StartBursting()
	{
		Log.Info( "About to spawn a burst of enemies." );

		// Give the player some time to prepare, unless it's the start of the wave.
		if ( SpawnBank != SpawnBankMax )
			NextSpawn = 5f;

		SpawningBurst = true;
		NextBurst = Random.Shared.Float( BurstDelayMin, BurstDelayMax );
	}


	public static Vector3 OutsidePosition()
	{
		return Rotation.FromYaw( Random.Shared.Float( 0, 360f ) ).Forward * 3000f;
	}

	/// <summary>
	/// Spawn a random enemy.
	/// </summary>
	public static float SpawnEnemy()
	{
		int r = Random.Shared.Int( 1, 130 );

		// Weighted Randomness
		if ( r <= 45 )
		{
			return SpawnSquare();
		}
		else if ( r <= 59 )
		{
			var size = Random.Shared.Float( 1.0f, 1.5f );
			return SpawnSnake( size );
		}
		else if ( r <= 100 )
		{
			return SpawnTriangle();
		}
		else
		{
			return SpawnGate();
		}
	}

	public static void SpawnEnemyBurst()
	{
		var r = Random.Shared.Int( 1, 4 );

		switch ( r )
		{
			case 1:
				SpawnTriangleBurst();
				break;
			case 2:
				SpawnSquareBurst();
				break;
			case 3:
				SpawnSnakeBurst();
				break;
			case 4:
				SpawnGateBurst();
				break;
		}
	}

	public static float SpawnTriangle()
	{
		var _ = new Triangle { Position = OutsidePosition() };
		return 5f; // Spawn cost.
	}

	public static float SpawnSquare()
	{
		var _ = new Square { Position = OutsidePosition() };
		return 10f; // Spawn cost.
	}

	public static float SpawnSnake( float size = 1.0f )
	{
		if ( Random.Shared.Int( 1, 20 ) == 1 )
			size *= 3; // subtle reference to my dong

		var s = new SnakeHead();
		s.Position = OutsidePosition();
		s.CreateBody( size );

		// Cost of spawning this wormy 'little' guy.
		return 10f * size;
	}

	public static float SpawnGate()
	{
		var g = new GateLine();
		g.Position = OutsidePosition();
		g.PositionNodes();

		return 8f; // Spawn cost.
	}

	public static void SpawnTriangleBurst()
	{
		for ( int i = 0; i < 15; i++ )
		{
			SpawnBank = MathF.Max( 0f, SpawnBank - SpawnTriangle() );
		}
	}

	public static void SpawnSquareBurst()
	{
		for ( int i = 0; i < 10; i++ )
		{
			SpawnBank = MathF.Max( 0f, SpawnBank - SpawnSquare() );
		}
	}

	public static void SpawnSnakeBurst()
	{
		for ( int i = 0; i < 8; i++ )
		{
			SpawnBank = MathF.Max( 0f, SpawnBank - SpawnSnake() );
		}
	}

	public static void SpawnGateBurst()
	{
		for ( int i = 0; i < 12; i++ )
		{
			SpawnBank = MathF.Max( 0f, SpawnBank - SpawnGate() );
		}
	}


	/// <summary>
	/// A client has joined the server.
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		var pawn = new Pawn();
		client.Pawn = pawn;
		pawn.Respawn();

		// TODO: Ingame Options & Mode Selection

		// Initialize Game
		// if ( GetPlayers().Count <= 1 )
		// Restart();
	}


	public static Vector3 MouseWorldPos()
	{
		var mRay = Camera.Main.GetRay( Mouse.Position );
		var plane = new Plane( Vector3.Zero, Vector3.Down );

		var hit = plane.TryTrace( mRay, out var hitPosition, twosided: true );
		if ( !hit ) return Vector3.Zero;

		return hitPosition;
	}


	public override void DoPlayerDevCam( IClient client )
	{
		base.DoPlayerDevCam( client );
	}

	/// <summary>
	/// Make the camera orthographic. Point it downwards.
	/// </summary>
	[GameEvent.Client.PostCamera]
	private static void OrthoCam()
	{
		var cam = Camera.Main;
		var devCam = Game.LocalClient.Components.Get<DevCamera>( true );

		if ( devCam != null && devCam.Enabled )
		{
			cam.Ortho = false;
			return;
		}

		cam.Ortho = true;
		cam.OrthoWidth = 1280;
		cam.OrthoHeight = 1280;
		cam.Rotation = Rotation.FromPitch( 90f );

		var pawn = Game.LocalClient.Pawn;

		if ( pawn.IsValid() )
			cam.Position = pawn.Position + Vector3.Up * 256f;

		Sound.Listener = new()
		{
			Position = cam.Position,
			Rotation = cam.Rotation
		};
	}
}

