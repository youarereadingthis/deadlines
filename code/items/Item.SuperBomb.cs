using System;
using Sandbox;

namespace DeadLines;

[Icon( "radio_button_checked" )]
public partial class ItemSuperBomb : Item
{
	public override string Name { get; set; } = "Super Bomb";
	public override string Description { get; set; } = "A bomb with twice the power.";

	public override bool Hidden { get; set; } = false;


	public override void OnUse( Pawn p )
	{
		var b = new Bomb();
		b.Position = p.Position;
		b.Explode( 1024f, 10f, 3f );

		Sound.FromEntity( To.Everyone, "player.bomb", b );
		// Sound.FromEntity( To.Everyone, "item.superbomb", b );

		base.OnUse( p );
	}
}
