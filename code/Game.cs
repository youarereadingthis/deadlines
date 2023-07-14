
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
	public bool GameNeverStarted { get; set; } = true;
	[Net]
	public float ArenaSize { get; set; } = 2048f;

	public static Vector2 ConstrainedMousePosition { get; set; } = Mouse.Position;

	public int ReadyPlayers
	{
		get
		{
			return Game.Clients
				.Where( x =>
				{
					var pawn = x.Pawn as Pawn;
					return pawn == null || pawn.Dead || !pawn.IsUpgradePanelOpen || pawn.Ready;
				} )
				.Count();
		}
	}

	/// <summary>
	/// When to end the slowmotion effect.
	/// </summary>
	/// <value></value>
	public TimeUntil SlowMotionEnd { get; set; } = 0f;


	public static DeadLines Manager => DeadLines.Current as DeadLines;


	[GameEvent.Tick.Server]
	public void Tick()
	{
		WaveLogic();
		AllDeadCheck();
		ReadyCheck();

		// Restore default timescale after TimeWatch is used.
		if ( !GameOver && Game.TimeScale < 1.0f && SlowMotionEnd )
		{
			// Log.Info( "Slow motion effect has ended." );
			Game.TimeScale = 1.0f;

			Sound.FromScreen( "item.time.stop" );
		}
	}

	private Vector2 _lastMPos = Vector2.Zero;

	[GameEvent.Client.Frame]
	public void Frame()
	{
		// Mouse.Delta doesn't work properly
		var actualDelta = Mouse.Position - _lastMPos;
		ConstrainedMousePosition = (ConstrainedMousePosition + actualDelta).Clamp( Vector2.Zero, Screen.Size );
		_lastMPos = Mouse.Position;
	}


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
		Manager.GameNeverStarted = false;

		StartWave( 0f );
		StartBursting();

		foreach ( Entity ent in Entity.All )
		{
			if ( ent is Enemy e )
			{
				e.Destroy( cleanup: true );
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

		// if (Manager.IsCoop)
		// SubmitCoopScores( Manager.Score );
		// else
		// SubmitScores( Manager.Score );
	}

	/// <summary>
	/// End the game if there are no pawns left.
	/// </summary>
	public void AllDeadCheck()
	{
		if ( !GameOver && AllDead() )
			GameEnd();
	}

	public void ReadyCheck()
	{
		if ( !GameOver && WaveOver && ReadyPlayers == Game.Clients.Count )
		{
			foreach ( var cl in Game.Clients )
			{
				if ( cl.Pawn is Pawn pawn )
				{
					pawn.HideUpgradeScreen();
					pawn.Ready = false;
				}
			}
			StartWave();
		}
	}

	[ClientRpc]
	public static void SubmitScores( int score )
	{
		Log.Info( "Submitted Score: " + score );
		Stats.SetValue( "highscore", score );
	}

	[ClientRpc]
	public static void SubmitCoopScores( int score )
	{
		Log.Info( "Submitted Score: " + score );
		Stats.SetValue( "highscorecoop", score );
	}

	[ClientRpc]
	public static void PlayerDied( IClient cl )
	{
		if ( cl == Game.LocalClient )
			Stats.Increment( "deaths", 1 );
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
		var mRay = Camera.Main.GetRay( ConstrainedMousePosition );
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

