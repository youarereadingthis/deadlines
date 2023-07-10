﻿
using System;
using Sandbox;

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
	public bool GameOver { get; set; } = false;
	[Net]
	public float ArenaSize { get; set; } = 2048f;


	public static DeadLines Manager => DeadLines.Current as DeadLines;


	/// <summary>
	/// Called when the game is created (on both the server and client)
	/// </summary>
	public DeadLines()
	{
		if ( Game.IsClient )
		{
			Game.RootPanel = new Hud();
		}
	}


	public static void ModifyScore( int score )
	{
		Manager.Score += score;
	}

	public static void Restart()
	{
		Manager.Score = 0;
		Manager.GameOver = false;

		NextSpawn = 0f;
		SpawnDelay = 8.0f;
		SpawnAmount = 5.0f;

		foreach ( Entity ent in Entity.All )
		{
			if ( ent is Pawn p )
			{
				p.Respawn( resetStats: true );
				p.Transform = new( Vector3.Zero, Rotation.Identity, 1 );
			}
			else if ( ent is Enemy e )
			{
				e.Destroy();
			}
		}
	}


	public static TimeUntil NextSpawn { get; set; } = 0f;
	public static float SpawnDelay { get; set; }
	public static float SpawnAmount { get; set; }


	[GameEvent.Tick.Server]
	public void EnemySpawner()
	{
		if ( NextSpawn )
		{
			NextSpawn = SpawnDelay;

			// Initial Enemies
			for ( int i = 0; i < SpawnAmount; i++ )
			{
				var sq = new Square();
				sq.Position = Rotation.FromYaw( Random.Shared.Float( 0, 360f ) ).Forward * 2048f;
			}

			SpawnDelay -= 0.25f; // have fun with that
			SpawnAmount += 0.25f;
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

		// Initialize Game
		Restart();
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
			cam.Position = pawn.Position + Vector3.Up * 512f;

		Sound.Listener = new()
		{
			Position = cam.Position,
			Rotation = cam.Rotation
		};
	}
}

