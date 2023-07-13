using System;
using Sandbox;

namespace DeadLines;

[Icon( "medical_services" )]
public partial class ItemHealthKit : Item
{
	public override string Name { get; set; } = "Health Kit";
	public override string Description { get; set; } = "Heals 4 of your health points.";

	public override bool Hidden { get; set; } = false;


	public override void OnUse( Pawn p )
	{
		if ( p.Health >= p.HealthMax )
			return;

		p.Health = MathF.Min( p.Health + 4f, p.HealthMax );

		// Sound.FromEntity( To.Everyone, "item.healthkit", p );

		base.OnUse( p );
	}
}