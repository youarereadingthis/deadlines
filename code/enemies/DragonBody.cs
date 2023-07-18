using System;
using Sandbox;

namespace DeadLines;


public class DragonBody : Enemy
{
	public override bool AutoDetect { get; set; } = false;

	public override int AddScore { get; set; } = 0;
	public override float BaseHealth { get; set; } = 80f;
	public override float Drag { get; set; } = 0.8f;
	public override Color Color { get; set; } = Color.Red;

	public override string HitSound { get; set; } = "hit4";

	public Enemy Head { get; set; } = null;
	public Enemy Follow { get; set; } = null;
	public float Distance { get; set; } = 120f;


	public override void Spawn()
	{
		SetModel( "models/vector/square.vmdl" );

		Scale = 2f;
		var hull = new BBox( Vector3.Zero, 64f ); // temp fix
		SetupPhysicsFromOBB( PhysicsMotionType.Keyframed, hull.Mins, hull.Maxs );

		base.Spawn();
	}


	public override void Tick()
	{
		base.Tick();

		Velocity -= (Velocity * Drag) * Time.Delta;
		Position = (Position + (Velocity * Time.Delta)).WithZ( 0 );

		if ( Follow.IsValid() )
		{
			var dist = Follow.Position.Distance( Position );
			if ( dist > Distance )
			{
				var dir = (Position - Follow.Position).Normal;
				Position = Follow.Position + (dir * Distance);
				Rotation = Rotation.From( dir.EulerAngles );
			}
		}
		else if ( !Destroyed )
		{
			Destroy();
		}
	}


	public override void OnTouch( Pawn p )
	{
	}

	public override void Hurt( float dmg )
	{
		// Head?.Hurt( dmg / 10f );

		// base.Hurt( dmg );
	}

	public override void Destroy( bool cleanup = false )
	{
		base.Destroy();

		// Follow = null;
	}
}