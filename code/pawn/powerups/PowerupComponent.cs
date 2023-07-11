using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadLines;
public abstract class PowerupComponent : EntityComponent<Pawn>
{
	public virtual void Toggle( bool? on = null ) { }
}
