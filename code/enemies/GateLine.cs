using System;
using Sandbox;

namespace DeadLines;


public class GateLine : Enemy
{
	public override int AddScore { get; set; } = 2;
	public override float BaseHealth { get; set; } = 5f;

	public override float Acceleration { get; set; } = 40f;
	public override float Drag { get; set; } = 4.0f;

	public override Color Color { get; set; } = Color.Orange;

	public override string HitSound { get; set; } = "hit3";

	public Rotation Direction { get; set; }
	public float TurnSpeed { get; set; } = 2.0f;

	public GateNode NodeLeft { get; set; }
	public GateNode NodeRight { get; set; }


	public override void Spawn()
	{
		SetModel( "models/vector/line.vmdl" );
		Scale = 2.0f;

		var size = new Vector3( 120f, 20f, 120f ) / 2f;
		var hull = new BBox( -size, size );
		SetupPhysicsFromOBB( PhysicsMotionType.Keyframed, hull.Mins, hull.Maxs );

		base.Spawn();

		// Create Nodes
		NodeLeft = CreateNode();
		NodeRight = CreateNode();
		PositionNodes();
	}


	public GateNode CreateNode()
	{
		var n = new GateNode();
		n.Position = Position;
		n.Follow = this;
		return n;
	}

	public void PositionNodes()
	{
		if ( Destroyed )
			return;

		var nodeDist = (32f * Scale) + 12f;

		// Left Node
		if ( NodeLeft.IsValid() && !NodeLeft.Destroyed )
			NodeLeft.Position = Position + (Rotation.Forward * nodeDist);

		// Right Node
		if ( NodeRight.IsValid() && !NodeRight.Destroyed )
			NodeRight.Position = Position + (Rotation.Backward * nodeDist);
	}

	public void BreakNodes()
	{
		if ( NodeLeft.IsValid() && !NodeLeft.Destroyed ) NodeLeft.Destroy();
		if ( NodeRight.IsValid() && !NodeRight.Destroyed ) NodeRight.Destroy();
		if ( !this.Destroyed ) Destroy();
	}


	public override void Tick()
	{
		base.Tick();

		if ( !Destroyed && ValidTarget() )
		{
			// Log.Info( "ValidTarget() = true" );
			var dir = (Player.Position.WithZ( 0 ) - Position.WithZ( 0 )).Normal;

			Direction = Rotation.LookAt( dir, Vector3.Up );

			Rotation = Rotation.Lerp( Rotation,
				Direction.RotateAroundAxis( Vector3.Up, 90f ),
				Time.Delta * TurnSpeed, true );

			Velocity += Rotation.RotateAroundAxis( Vector3.Up, -90f ).Forward * Acceleration * Time.Delta;
		}

		Velocity -= (Velocity * Drag) * Time.Delta;
		Position = (Position + Velocity).WithZ( 0 );

		PositionNodes();
	}
}