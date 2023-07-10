using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadLines;
public class BeamEntity : Entity
{
	private const float BEAM_LIFESPAN = .3f;
	private const float BEAM_THICKNESS = 5f;
	private static readonly Vector3 BEAM_COLOR = new Vector3( 255 );

	private Particles _beam;
	private Vector3 _endPos;
	private Vector3 _startPos;

	public Vector3 EndPosition
	{
		get
		{
			return _endPos;
		}
		set
		{
			_beam.SetPosition( 1, value );
			_endPos = value;
		}
	}

	public Vector3 StartPosition
	{
		get
		{
			return _startPos;
		}
		set
		{
			_beam.SetPosition( 0, value );
			_startPos = value;
		}
	}

	private TimeUntil _timeUntilDestroy = BEAM_LIFESPAN;

	public override void Spawn()
	{
		_beam = Particles.Create( "particles/beams/default_beam.vpcf" );

		_beam.SetPosition( 2, BEAM_COLOR );
		_beam.SetPosition( 3, new Vector3( BEAM_THICKNESS, 1, 1 ) );
	}

	[GameEvent.Tick.Server]
	private void Tick()
	{
		var fraction = _timeUntilDestroy.Relative / BEAM_LIFESPAN;
		_beam.SetPosition( 3, new Vector3( BEAM_THICKNESS * fraction, fraction, 1 ) );
		if ( _timeUntilDestroy )
		{
			_beam.Destroy( true );
			Delete();
		}
	}
}
