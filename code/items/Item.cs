using System;
using Sandbox;

namespace DeadLines;


[Category( "Items" )]
[Icon( "category" )]
[Description( "Lorem ipsum or however it goes. Extra text for length." )]
public partial class Item : BaseNetworkable
{
	public virtual string Name { get; set; } = "DEBUG ITEM";

	public virtual bool Hidden { get; set; } = true;


	public virtual void OnUse( Pawn p )
	{
		// Log.Info( "Item \"" + Name + "\" was used." );

		p.Item = null;
	}
}
