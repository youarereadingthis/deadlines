using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeadLines;
public static class PowerupComponentExtensions
{
	public static IReadOnlyDictionary<string, StatDescription> GetStatDescriptions( this PowerupComponent comp )
	{
		var result = new Dictionary<string, StatDescription>();
		var type = TypeLibrary.GetType( comp.GetType() );
		foreach ( var prop in type.Properties )
		{
			var statDesc = prop.GetCustomAttribute<StatDescription>();
			if ( statDesc != null )
			{
				result.Add( prop.Name, statDesc );
			}
		}
		return result.AsReadOnly();
	}
}
