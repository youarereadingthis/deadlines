
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Sandbox;
using Sandbox.Physics;
using Sandbox.Services;

namespace DeadLines;


public partial class DeadLines : Sandbox.GameManager
{
	[Net]
	public int WaveCount { get; set; } = 0;
	[Net]
	public TimeUntil NextWave { get; set; } = 0f;
	public static float SpawnBank { get; set; } = 100f;
	public static float SpawnBankMax { get; set; } = 100f;
	public static float SpawnBankBase { get; set; } = 350f;

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
	public static float BurstDelayMin { get; set; } = 40.0f;
	public static float BurstDelayMax { get; set; } = 80.0f;

	/// <summary>
	/// Are we during an active burst?
	/// </summary>
	public static bool SpawningBurst { get; set; } = false;
	public static TimeUntil BurstEnd { get; set; } = 0f;

	/// <summary>
	/// Should the next spawn be a burst?
	/// </summary>
	public static bool ShouldBurst { get; set; } = false;


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
			else if ( SpawningBurst )
			{
				// Actively spawning a burst.
				if ( BurstEnd || EnemyCount() == 0 )
				{
					NextSpawn = 0;
					SpawningBurst = false;
				}
			}
			else if ( NextSpawn || EnemyCount() == 0 )
			{
				if ( ShouldBurst )
				{
					// Give them some time to deal with the burst.
					BurstEnd = 10f;
					SpawningBurst = true;
					ShouldBurst = false;

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
					NextSpawn = MathX.Lerp( SpawnDelayMax, SpawnDelayMin, frac, true ) / pCount;
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
		SpawnBankMax = (SpawnBankBase + (Manager.WaveCount * 50f)) * pCount;
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
		Log.Info( "Spawning a burst of enemies." );

		ShouldBurst = true;
		NextBurst = Random.Shared.Float( BurstDelayMin, BurstDelayMax );
	}


	public static Vector3 OutsidePosition()
	{
		return Rotation.FromYaw( Random.Shared.Float( 0, 360f ) ).Forward * 2500f;
	}

	/// <summary>
	/// Spawn a random enemy.
	/// </summary>
	public static float SpawnEnemy()
	{
		int r = Random.Shared.Int( 1, 155 );

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
		else if ( r <= 130 )
		{
			return SpawnGate();
		}
		else
		{
			return SpawnBlob();
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

	public static float SpawnBlob( float scale = 1.0f )
	{
		var s = new Blob();
		s.Position = OutsidePosition();
		s.SetScale( s.DefaultScale * scale );

		return 10f * scale;
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



}