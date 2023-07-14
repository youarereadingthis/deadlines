using System;
using Sandbox;

namespace DeadLines;


[Category( "Items" )]
[Icon( "category" )]
public partial class Item : BaseNetworkable
{
	public virtual string Name { get; set; } = "DEBUG ITEM";
	public virtual string Description { get; set; } = "Lorem ipsum or however it goes. Extra text for length.";

	public virtual bool Hidden { get; set; } = true;


	public virtual void OnUse( Pawn p )
	{
		// Log.Info( "Item \"" + Name + "\" was used." );

		p.Item = null;
	}
}
