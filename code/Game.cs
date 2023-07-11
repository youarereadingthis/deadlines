
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Sandbox;
using Sandbox.Physics;

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

	public static uint EnemyCount()
	{
		uint count = 0;

		foreach ( Entity ent in Entity.All )
		{
			if ( ent is Enemy e )
				count++;
		}

		return count;
	}

	public static bool AllDead()
	{
		foreach ( var cl in Game.Clients )
		{
			if ( cl.Pawn is Pawn p && !p.Dead )
				return false;
		}

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

		foreach ( Entity ent in Entity.All )
		{
			if ( ent is Enemy e )
			{
				e.Destroy();
			}
			else if ( ent is ChainBall b )
			{
				b.Player = null;
				b.Disconnected = true;
			}
		}

		foreach ( Pawn p in GetPlayers() )
		{
			p.Respawn( resetStats: true );
			p.Transform = new( Vector3.Zero, Rotation.Identity, 1 );
		}
	}

	public static void GameEnd()
	{
		Game.TimeScale = 0.5f;
		Manager.GameOver = true;
	}


	[Net]
	public int WaveCount { get; set; } = 0;
	public static TimeUntil NextWave { get; set; } = 0f;
	public static float SpawnBank { get; set; } = 100f;
	public static float SpawnBankMax { get; set; } = 100f;

	public static float SpawnDelayMax { get; set; } = 2.0f;
	public static float SpawnDelayMin { get; set; } = 0.5f;
	public static TimeUntil NextSpawn { get; set; } = 0f;


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
			// Continuously spawn until we're outta juice.
			// Then wait until the next wave.

			if ( SpawnBank <= 0f )
			{
				if ( EnemyCount() == 0 )
					FinishWave();
			}
			else if ( NextSpawn )
			{
				// How far is the wave along?
				var frac = SpawnBank / SpawnBankMax;

				// Spawn faster as the wave proceeds.
				NextSpawn = MathX.Lerp( SpawnDelayMin, SpawnDelayMax, frac, true );
				// Spawn the enemy and subtract its value from our bank.
				SpawnBank = MathF.Max( 0f, SpawnBank - SpawnEnemy() );
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
					p.Health = p.MaxHealth;
			}
		}

		Log.Info( "You survived wave " + Manager.WaveCount + "!" );
		StartWave( 3f );
	}

	public static void StartWave( float delay = 10f )
	{
		// TODO: Upgrade selection screen once enemies are dead.

		SpawnBankMax = 500f + (Manager.WaveCount * 100f);
		SpawnBank = SpawnBankMax;

		NextWave = delay;
		NextSpawn = 0f;

		Manager.WaveCount++;
	}


	public static Vector3 OutsidePosition()
	{
		return Rotation.FromYaw( Random.Shared.Float( 0, 360f ) ).Forward * 3000f;
	}

	/// <summary>
	/// Spawn a random enemy.
	/// </summary>
	public float SpawnEnemy()
	{
		int r = Random.Shared.Int( 1, 100 );

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
		else
		{
			return SpawnTriangle();
		}
	}

	public static float SpawnSquare()
	{
		var _ = new Square
		{
			Position = OutsidePosition()
		};

		return 10f; // Spawn cost.
	}

	public static float SpawnTriangle()
	{
		var _ = new Triangle
		{
			Position = OutsidePosition()
		};

		return 5f; // Spawn cost.
	}

	public static float SpawnSnake( float size = 1.0f )
	{
		var s = new SnakeHead();
		s.Position = OutsidePosition();
		s.CreateBody( size );

		// Cost of spawning this wormy 'little' guy.
		return 10f * size;
	}


	/// <summary>
	/// End the game if there are no pawns left.
	/// </summary>
	public void AllDeadCheck()
	{
		if ( !GameOver && AllDead() )
		{
			GameEnd();
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

