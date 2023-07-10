using Sandbox;
using System;

namespace DeadLines;

public partial class PawnController : EntityComponent<Pawn>
{

	protected static Vector3 IntersectPlane( Vector3 pos, Vector3 dir, float z )
	{
		float a = (z - pos.z) / dir.z;
		return new( dir.x * a + pos.x, dir.y * a + pos.y, z );
	}

	protected static Rotation LookAt( Vector3 targetPosition, Vector3 position )
	{
		var targetDelta = (targetPosition - position);
		var direction = targetDelta.Normal;

		return Rotation.From( new Angles(
			((float)Math.Asin( direction.z )).RadianToDegree() * -1.0f,
			((float)Math.Atan2( direction.y, direction.x )).RadianToDegree(),
			0.0f ) );
	}


	public void Simulate( IClient cl )
	{
		/*var movement = Entity.InputDirection.Normal;
		var angles = Vector3.Forward.EulerAngles;
		var moveVector = Rotation.From( angles ) * movement * 1000f;

		Entity.Velocity = Accelerate( Entity.Velocity, moveVector.Normal, moveVector.Length, MoveSpeed, 20f );
		Entity.Velocity = ApplyFriction( Entity.Velocity, 4.0f );*/

		var moveDir = Entity.InputDirection.Normal;

		Entity.Velocity = new Vector3( moveDir.x, moveDir.y, 0f ) * Entity.MoveSpeed;
		Entity.Position += Entity.Velocity * Time.Delta;

		// Arena Bounds
		var size = DeadLines.Manager.ArenaSize / 2;
		var mins = new Vector3( -size ).WithZ( 0 );
		var maxs = new Vector3( size ).WithZ( 0 );
		Entity.Position = Entity.Position.Clamp( mins, maxs );

		DebugOverlay.Line( mins, mins.WithY( maxs.y ), Color.Gray, 0.04f ); // south
		DebugOverlay.Line( maxs, maxs.WithY( mins.y ), Color.Gray, 0.04f ); // north
		DebugOverlay.Line( mins, mins.WithX( maxs.x ), Color.Gray, 0.04f ); // north
		DebugOverlay.Line( maxs, maxs.WithX( mins.x ), Color.Gray, 0.04f ); // north}
	}


	Vector3 ApplyFriction( Vector3 input, float frictionAmount )
	{
		float StopSpeed = 100.0f;

		var speed = input.Length;
		if ( speed < 0.1f ) return input;

		// Bleed off some speed, but if we have less than the bleed
		// threshold, bleed the threshold amount.
		float control = (speed < StopSpeed) ? StopSpeed : speed;

		// Add the amount to the drop amount.
		var drop = control * Time.Delta * frictionAmount;

		// scale the velocity
		float newspeed = speed - drop;
		if ( newspeed < 0 ) newspeed = 0;
		if ( newspeed == speed ) return input;

		newspeed /= speed;
		input *= newspeed;

		return input;
	}

	Vector3 Accelerate( Vector3 input, Vector3 wishdir, float wishspeed, float speedLimit, float acceleration )
	{
		if ( speedLimit > 0 && wishspeed > speedLimit )
			wishspeed = speedLimit;

		var currentspeed = input.Dot( wishdir );
		var addspeed = wishspeed - currentspeed;

		if ( addspeed <= 0 )
			return input;

		var accelspeed = acceleration * Time.Delta * wishspeed;

		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		input += wishdir * accelspeed;

		return input;
	}
}
