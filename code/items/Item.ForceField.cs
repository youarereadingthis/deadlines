using System;
using Sandbox;

namespace DeadLines;

[Icon( "shield" )]
public partial class ItemForceField : Item
{
	public override string Name { get; set; } = "Force Field";
	public override string Description { get; set; } = "Keeps enemies away for 10 seconds.";

	public override bool Hidden { get; set; } = false;


	public override void OnUse( Pawn p )
	{
		var b = new Bomb();
		b.Position = p.Position;
		b.Explode( 512f, 0, 10f );

		// Sound.FromEntity( To.Everyone, "item.forcefield", this );

		base.OnUse( p );
	}
}
