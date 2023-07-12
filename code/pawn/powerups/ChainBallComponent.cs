﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadLines;
public class ChainBallComponent : PowerupComponent
{
	private ChainBall _chainBall;

	private float _level;

	[StatDescription( Name = "Ball Power" )]
	public float Level
	{
		get
		{
			return _level;
		}
		set
		{
			if ( _chainBall == null )
				return;

			var diff = value - _level;
			_chainBall.BallScale += diff * .1f;
			_chainBall.HitForce += diff * 10;
			_level = value;
		}
	}

	// Allows for a temporary "graceful shutdown" of the component's features
	public override void Toggle( bool? on = null )
	{
		if ( _chainBall.IsValid() )
		{
			_chainBall.Active = on ?? !_chainBall.Active;
		}
	}

	protected override void OnActivate()
	{
		if ( !Game.IsServer )
			return;
		_chainBall = new ChainBall();
		_chainBall.Follow = Entity;
	}

	protected override void OnDeactivate()
	{
		if ( !Game.IsServer )
			return;

		if ( _chainBall != null )
		{
			// ChainBall will delete itself if there's no Follow
			_chainBall.Follow = null;
		}
	}
}
