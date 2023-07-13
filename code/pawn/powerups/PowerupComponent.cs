using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadLines;
public abstract partial class PowerupComponent : EntityComponent<Pawn>
{
	public virtual void Toggle( bool? on = null ) { }

	public IReadOnlyDictionary<string, StatDescription> StatDescriptions;

	[Net]
	public IDictionary<string, int> Upgrades { get; set; }

	[Net]
	public IList<string> AvailableUpgrades { get; set; }

	public PowerupComponent()
	{
		StatDescriptions = this.GetStatDescriptions();
		this.ResetStats();
	}

	[ConCmd.Server]
	public static void AddPowerupUpgradeCmd( string compType, string propertyName )
	{
		var pawn = ConsoleSystem.Caller.Pawn as Pawn;
		if ( !pawn.IsValid() )
			return;

		var comp = pawn.Components.GetAll<PowerupComponent>()
			.FirstOrDefault( x => x.GetType().ToString() == compType );

		if ( comp == null )
			return;

		comp.AddUpgrade( propertyName );
	}

	public void AddUpgrade( string propertyName )
	{
		if ( !Game.IsServer )
			return;

		if ( Entity.UpgradePoints <= 0 )
			return;


		this.IncrementStat( propertyName );
		Upgrades.TryGetValue( propertyName, out var statUpgradePoints );
		statUpgradePoints++;
		Upgrades[propertyName] = statUpgradePoints;
		Entity.UpgradePoints--;

		StatDescriptions.TryGetValue( propertyName, out var desc );
		if ( desc != null && statUpgradePoints >= desc.MaxPoints && AvailableUpgrades.Contains( propertyName ) )
			AvailableUpgrades.Remove( propertyName );
	}
}
