using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadLines;

public partial class DeadLines
{
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
		if ( !pawn.IsValid() )
			return;
		pawn.GodMode = !pawn.GodMode;
		Log.Info( "God mode is " + (pawn.GodMode ? "ON" : "OFF") );
	}

	[ConCmd.Server( "dl_toggle_upgrade_panel" )]
	public static void ToggleUpgradePanelCmd()
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( pawn.IsValid() && !pawn.Dead )
		{
			pawn.IsUpgradePanelOpen = !pawn.IsUpgradePanelOpen;
		}
	}

	[ConCmd.Server( "dl_give_upgrade_points" )]
	public static void GiveUpgradePoints( int points )
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( pawn.IsValid() )
			pawn.UpgradePoints += points;
	}

	[ConCmd.Admin( "dl_wave_finish" )]
	public static void FinishWaveCmd()
	{
		FinishWave();
	}

	[ConCmd.Admin( "dl_wave_set" )]
	public static void SetWaveCmd(int wave)
	{
		Manager.WaveCount = wave;
	}
}