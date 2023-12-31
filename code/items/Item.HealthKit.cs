using System;
using Sandbox;

namespace DeadLines;

[Title( "Health Kit" ), Description( "Heals 4 health points." ), Icon( "medical_services" )]
public partial class ItemHealthKit : Item
{
	public override string Name { get; set; } = "Health Kit";
	public override bool Hidden { get; set; } = false;


	public override void OnUse( Pawn p )
	{
		if ( p.Health >= p.HealthMax )
			return;

		p.Health = MathF.Min( p.Health + 4f, p.HealthMax );

		Sound.FromEntity( To.Everyone, "item.heal", p );

		base.OnUse( p );
	}
}
