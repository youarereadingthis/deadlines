using System;
using Sandbox;

namespace DeadLines;


public partial class ItemTimeWatch : Item
{
	public override string Name { get; set; } = "Time Watch";
	public override string Description { get; set; } = "Slows time time for a while.";

	public override bool Hidden { get; set; } = false;


	public override void OnUse( Pawn p )
	{
		if ( Game.TimeScale < 1.0f )
			return;

		Game.TimeScale = 0.5f;
		DeadLines.Manager.SlowMotionEnd = 10f * Game.TimeScale;

		// Sound.FromEntity( To.Everyone, "item.timewatch", p );

		base.OnUse( p );
	}
}