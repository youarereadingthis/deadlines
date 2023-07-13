using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadLines;

[AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
public class StatDescription : Attribute
{
	public const string DefaultIcon = "upgrade";
	/// <summary>
	/// The user-friendly name of this stat to be shown in the UI.
	/// </summary>
	public string Name = "";
	/// <summary>
	/// The default value of this stat.
	/// </summary>
	public float Default = 0;
	/// <summary>
	/// The minimum value of this stat.
	/// </summary>
	public float Min = 1;
	/// <summary>
	/// The maximum value of this stat.
	/// </summary>
	public float Max = 999999999;
	/// <summary>
	/// Whether or not this stat will be shown in the shop.
	/// </summary>
	public bool Upgradeable = true;
	/// <summary>
	/// The amount to add to this stat with every point spent on it. Can be negative.
	/// </summary>
	public float UpgradeIncrement = 1;
	/// <summary>
	/// The icon to display for this upgrade - see https://fonts.google.com/icons?selected=Material+Icons
	/// </summary>
	public string Icon { get; set; } = DefaultIcon;

	private int _maxPoints = 0;
	/// <summary>
	/// The maximum number of points the player can spend on this stat. Defaults to the lesser of 100 and Max / UpgradeIncrement.
	/// </summary>
	public int MaxPoints
	{
		get
		{
			if ( _maxPoints > 0 )
				return _maxPoints;

			// "Default" MaxPoints if not specified
			var limit = UpgradeIncrement > 0 ? Max : Min;
			var diff = Math.Abs( limit - Default ) - .0000001;
			_maxPoints = (int)Math.Min( 100, Math.Ceiling( diff / Math.Abs( UpgradeIncrement ) ) );
			return _maxPoints;
		}
		set
		{
			_maxPoints = value;
		}
	}
}
