using System;
using Sandbox;

namespace DeadLines;

[Title( "Super Bomb" ), Description( "A bomb with 3x the radius." ), Icon( "radio_button_checked" )]
public partial class ItemSuperBomb : Item
{
	public override string Name { get; set; } = "Super Bomb";
	public override bool Hidden { get; set; } = false;


	public override void OnUse( Pawn p )
	{
		var b = new Bomb();
		b.Position = p.Position;
		b.Explode( 512f * 3f, 20f, 6f );

		Sound.FromEntity( To.Everyone, "item.bomb", b );

		base.OnUse( p );
	}
}
