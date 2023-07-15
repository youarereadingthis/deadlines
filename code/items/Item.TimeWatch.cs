using System;
using Sandbox;

namespace DeadLines;

[Title( "Time Watch" ), Description( "Temporary slow motion." ), Icon( "watch" )]
public partial class ItemTimeWatch : Item
{
	public override string Name { get; set; } = "Time Watch";
	public override bool Hidden { get; set; } = false;


	public override void OnUse( Pawn p )
	{
		if ( Game.TimeScale < 1.0f )
			return;

		Game.TimeScale = 0.5f;
		DeadLines.Manager.SlowMotionEnd = 15f * Game.TimeScale;

		Sound.FromScreen( "item.time.start" );

		base.OnUse( p );
	}
}
