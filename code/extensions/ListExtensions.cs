using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadLines;
public static class ListExtensions
{
	public static void Shuffle<T>( this IList<T> list )
	{
		int n = list.Count;
		while ( n > 1 )
		{
			n--;
			int k = Game.Random.Next( n + 1 );
			T value = list[k];
			list[k] = list[n];
			list[n] = value;
		}
	}

	public static T? Pop<T>( this IList<T> list ) where T : struct
	{
		if ( list.Count() == 0 )
			return null;

		var result = list[list.Count() - 1];
		list.RemoveAt( list.Count() - 1 );
		return result;
	}
}
