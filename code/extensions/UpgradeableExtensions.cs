using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeadLines;
public static class UpgradeableExtensions
{
	public static List<(string PropName, IList<string> AvailableList)> GetUpgradeableStatPropNames( this Pawn pawn )
	{
		var result = new List<(string PropName, IList<string> AvailableList)>();
		foreach ( var pair in Pawn.StatDescriptions )
		{
			if ( !pair.Value.Upgradeable )
				continue;

			pawn.Upgrades.TryGetValue( pair.Key, out var points );
			if ( points >= pair.Value.MaxPoints )
				continue;

			result.Add( new( pair.Key, pawn.AvailableUpgrades ) );
		}
		return result;
	}

	public static List<(string PropName, IList<string> AvailableList)> GetUpgradeableStatPropNames( this PowerupComponent comp )
	{
		var result = new List<(string PropName, IList<string> AvailableList)>();
		foreach ( var pair in comp.StatDescriptions )
		{
			if ( !pair.Value.Upgradeable )
				continue;

			comp.Upgrades.TryGetValue( pair.Key, out var points );
			if ( points >= pair.Value.MaxPoints )
				continue;

			result.Add( new( pair.Key, comp.AvailableUpgrades ) );
		}
		return result;
	}

	public static void IncrementStat( this Pawn pawn, string propName )
	{
		var found = Pawn.StatDescriptions.TryGetValue( propName, out var statDesc );
		if ( !found )
		{
			Log.Error( $"AddUpgrade error: StatDescription not found for property {propName} on Pawn" );
			return;
		}

		pawn.IncrementStat( propName, statDesc );
	}

	public static void IncrementStat( this PowerupComponent comp, string propName )
	{
		var found = comp.StatDescriptions.TryGetValue( propName, out var statDesc );
		if ( !found )
		{
			Log.Error( $"AddUpgrade error: StatDescription not found for property {propName} on PowerupComponent" );
			return;
		}

		comp.IncrementStat( propName, statDesc );
	}

	public static void ResetStats( this Pawn pawn )
	{
		foreach ( var pair in Pawn.StatDescriptions )
		{
			pawn.SetStat( pair.Key, pair.Value.Default );
		}
	}

	public static void ResetStats( this PowerupComponent comp )
	{
		foreach ( var pair in comp.StatDescriptions )
		{
			comp.SetStat( pair.Key, pair.Value.Default );
		}
	}

	private static void IncrementStat( this object obj, string propName, StatDescription statDesc )
	{
		var val = TypeLibrary.GetPropertyValue( obj, propName );
		if ( val == null )
		{
			Log.Error( $"IncrementStat error: Property {propName} not found on {obj.GetType()}" );
			return;
		}

		switch ( val )
		{
			case int tInt:
				tInt += (int)statDesc.UpgradeIncrement;
				val = Math.Max( (int)statDesc.Min, Math.Min( (int)statDesc.Max, tInt ) );
				break;
			case float tFloat:
				tFloat += statDesc.UpgradeIncrement;
				val = Math.Max( statDesc.Min, Math.Min( statDesc.Max, tFloat ) );
				break;
		}

		TypeLibrary.SetProperty( obj, propName, val );
	}

	private static void SetStat( this object obj, string propName, float value )
	{
		var val = TypeLibrary.GetPropertyValue( obj, propName );
		switch ( val )
		{
			case int:
				val = (int)value;
				break;
			case float:
				val = value;
				break;
		}
		TypeLibrary.SetProperty( obj, propName, val );
	}
}
