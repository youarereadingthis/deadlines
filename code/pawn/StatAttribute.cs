using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadLines;

[AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
public class StatDescription : Attribute
{
	public string Name = "";
	public float Default = 0;
	public float Min = 1;
	public float Max = 999999999;
	public bool Upgradeable = true;
	public float UpgradeIncrement = 1;
	public int ShopOrder = int.MaxValue;
}
