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

	[ConCmd.Server( "toggle_upgrade_panel" )]
	public static void ToggleUpgradePanelCmd()
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( pawn.IsValid() && !pawn.Dead )
		{
			pawn.IsUpgradePanelOpen = !pawn.IsUpgradePanelOpen;
		}
	}

	[ConCmd.Server( "give_upgrade_points" )]
	public static void GiveUpgradePoints( int points )
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( pawn.IsValid() )
			pawn.UpgradePoints += points;
	}

	[ConCmd.Admin( "finish_wave" )]
	public static void FinishWaveCmd()
	{
		DeadLines.FinishWave();
	}
}
