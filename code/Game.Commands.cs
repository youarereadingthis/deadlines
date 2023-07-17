using Sandbox;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadLines;

public partial class DeadLines
{
	private static List<long> DevSteamIDs = new()
	{
		76561198043583453,
		76561197998344127
	};

	private static bool DevCheck()
	{
		return DevSteamIDs.Contains( ConsoleSystem.Caller.SteamId );
	}

	[ConCmd.Server( "kill" )]
	public static void KillCmd()
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( pawn.IsValid() && !pawn.Dead )
		{
			pawn.Health = 0;
			pawn.Die();
		}
	}

	[ConCmd.Admin( "god" )]
	public static void GodCmd()
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( !pawn.IsValid() || !DevCheck() )
			return;

		pawn.GodMode = !pawn.GodMode;
		Log.Info( "God mode is " + (pawn.GodMode ? "ON" : "OFF") );
	}

	[ConCmd.Server( "dl_toggle_upgrade_panel" )]
	public static void ToggleUpgradePanelCmd()
	{
		if ( !DevCheck() )
			return;

		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( pawn.IsValid() && !pawn.Dead )
		{
			pawn.IsUpgradePanelOpen = !pawn.IsUpgradePanelOpen;
		}
	}

	[ConCmd.Server( "dl_give_upgrade_points" )]
	public static void GiveUpgradePoints( int points )
	{
		if ( !DevCheck() )
			return;

		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( pawn.IsValid() )
			pawn.UpgradePoints += points;
	}

	[ConCmd.Admin( "dl_wave_finish" )]
	public static void FinishWaveCmd()
	{
		if ( !DevCheck() )
			return;

		FinishWave();
	}

	[ConCmd.Admin( "dl_wave_set" )]
	public static void SetWaveCmd( int wave )
	{
		if ( !DevCheck() )
			return;

		Manager.WaveCount = wave;
	}

	[ConCmd.Admin( "dl_health" )]
	public static void SetHealthCmd( int hp = 4 )
	{
		if ( !DevCheck() )
			return;

		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( pawn.IsValid() && !pawn.Dead )
		{
			pawn.Health = hp;
		}
	}

	[ConCmd.Admin( "dl_give_ball" )]
	public static void GiveBallCmd()
	{
		if ( !DevCheck() )
			return;

		var pawn = ConsoleSystem.Caller.Pawn;
		if ( pawn.IsValid() )
		{
			pawn.Components.Add( new ChainBallComponent() );
		}
	}

	[ConCmd.Admin( "dl_cleanup" )]
	public static void CleanupCmd()
	{
		if ( !DevCheck() )
			return;

		DeadLines.CleanupEnemies();
	}
}
