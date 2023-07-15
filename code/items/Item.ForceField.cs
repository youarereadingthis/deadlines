using System;
using Sandbox;

namespace DeadLines;

[Title( "Force Field" ), Description( "Keeps enemies away for 10 seconds." ), Icon( "shield" )]
public partial class ItemForceField : Item
{
	public override string Name { get; set; } = "Force Field";
	public override bool Hidden { get; set; } = false;


	public override void OnUse( Pawn p )
	{
		var b = new Bomb();
		b.Position = p.Position;
		b.Explode( 512f, 0, 15f );

		Sound.FromEntity( To.Everyone, "item.shield", b );

		base.OnUse( p );
	}
}
