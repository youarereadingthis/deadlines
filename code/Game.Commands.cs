using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadLines;

public partial class DeadLines
{
	[ConCmd.Server("kill")]
	public static void KillCmd()
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if (pawn.IsValid() && !pawn.Dead)
		{
			pawn.Health = 0;
			pawn.Die();
		}
	}
}
