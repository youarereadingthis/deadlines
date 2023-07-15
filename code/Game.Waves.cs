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
	public static bool WaveOver { get; set; }
	public static TimeUntil WaveEnd { get; set; } = 0f;
	public static float WaveBaseDuration { get; set; } = 60f;
	public static float WaveCountDuration { get; set; } = 2f;

	// Level of challenge, per wave.
	public static float IntensityMin { get; set; }
	public static float IntensityMax { get; set; }
	public static float IntensityLimit { get; set; } = 500f;
	public static float BaseIntensity { get; set; } = 100f;
	public static float MostIntenseWave { get; set; } = 17f;

	// Global limit of spawn rates.
	public static float SpawnDelayMin { get; set; } = 0.1f;
	public static float SpawnDelayMax { get; set; } = 1.5f;
	public static TimeUntil NextSpawn { get; set; } = 0f;

	// Sometimes spawn bursts of enemies.
	public static TimeUntil NextBurst { get; set; } = 0f;
	public static float BurstDelayMin { get; set; } = 45.0f;
	public static float BurstDelayMax { get; set; } = 70.0f;

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


	public void WaveLogic()
	{
		if ( GameOver )
			return;

		if ( !WaveOver )
		{
			foreach ( Pawn p in GetPlayers() )
			{
				p.HideUpgradeScreen();
			}

			// Continuously spawn until the wave is over.
			Log.Info( "WaveFraction() = " + WaveFraction() );

			if ( WaveFraction() >= 1f )
			{
				if ( EnemyCount() == 0 )
					FinishWave();
			}
			else if ( NextBurst )
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
					float skill = MathX.Lerp( IntensityMin, IntensityMax, WaveFraction(), true );
					float frac = MathX.Clamp( skill / IntensityLimit, 0f, 1f );
					// Log.Info( "skill:" + skill );
					// Log.Info( "frac:" + frac );

					// Spawn faster as the wave proceeds.
					var pCount = MathF.Max( 1f, PlayerCount() );
					NextSpawn = MathX.Lerp( SpawnDelayMax, SpawnDelayMin, frac, true ) / pCount;
					// Log.Info( "NextSpawn:" + NextSpawn );
					// Spawn the enemy and subtract its value from our bank.
					SpawnEnemy();
				}
			}
		}
	}

	/// <summary>
	/// A fraction of how far along the wave is.
	/// </summary>
	public static float WaveFraction()
	{
		return WaveEnd.Fraction;
	}

	public static void FinishWave()
	{
		WaveOver = true;

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

			foreach ( var leaverData in Manager._leaverData )
			{
				leaverData.Value.ShouldRespawn = true;
			}
		}

		Log.Info( "You survived wave " + Manager.WaveCount + "!" );
	}

	public static void StartWave( float delay = 10f )
	{
		// Reach max intensity at a certain level.
		// From then on, only the minimum intensity may increase.
		var frac = Manager.WaveCount / MostIntenseWave;
		IntensityMin = MathF.Min( IntensityLimit, (IntensityLimit * frac) * 0.5f );
		IntensityMax = MathF.Min( IntensityLimit, BaseIntensity + (IntensityLimit * frac) );

		// Spawn more enemies per wave.
		var pCount = MathF.Max( 1f, PlayerCount() );
		WaveEnd = WaveBaseDuration + (Manager.WaveCount * WaveCountDuration);

		// Log.Info( "IntensityMin:" + IntensityMin );
		// Log.Info( "IntensityMax:" + IntensityMax );

		Manager.NextWave = delay;
		NextSpawn = 0f;

		Manager.WaveCount++;
		WaveOver = false;
	}

	public static void StartBursting()
	{
		// Log.Info( "Spawning a burst of enemies." );

		// Give the player just a bit of time to react.
		if ( WaveFraction() != 0f )
			NextSpawn = 3f;

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
			// Make blobs mega sometimes.
			if ( Random.Shared.Int( 1, 5 ) == 5 )
				return SpawnBlob( 2 );
			else
				return SpawnBlob();
		}
	}

	public static void SpawnEnemyBurst()
	{
		var r = Random.Shared.Int( 1, 4 );
		var skill = Manager.WaveCount / MostIntenseWave;
		var pCount = MathF.Max( 1f, PlayerCount() );
		var scale = 1f + (skill * 1f * pCount);

		switch ( r )
		{
			case 1:
				SpawnTriangleBurst( scale );
				break;
			case 2:
				SpawnSquareBurst( scale );
				break;
			case 3:
				SpawnSnakeBurst( scale );
				break;
			case 4:
				SpawnGateBurst( scale );
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

	public static void SpawnTriangleBurst( float scale = 1f )
	{
		for ( int i = 0; i < 15 * scale; i++ )
		{
			SpawnTriangle();
		}
	}

	public static void SpawnSquareBurst( float scale = 1f )
	{
		for ( int i = 0; i < 10 * scale; i++ )
		{
			SpawnSquare();
		}
	}

	public static void SpawnSnakeBurst( float scale = 1f )
	{
		for ( int i = 0; i < 7 * scale; i++ )
		{
			SpawnSnake();
		}
	}

	public static void SpawnGateBurst( float scale = 1f )
	{
		for ( int i = 0; i < 12 * scale; i++ )
		{
			SpawnGate();
		}
	}
}
