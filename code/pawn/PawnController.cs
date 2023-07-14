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

		// DebugOverlay.Circle( Vector3.Zero + Vector3.Down * 32f, Rotation.From( Vector3.Down.EulerAngles ), 4f, Color.Gray, 0.04f, false );
		DebugOverlay.Line( mins, mins.WithY( maxs.y ), Color.Gray, 0.04f, false );
		DebugOverlay.Line( maxs, maxs.WithY( mins.y ), Color.Gray, 0.04f, false );
		DebugOverlay.Line( mins, mins.WithX( maxs.x ), Color.Gray, 0.04f, false );
		DebugOverlay.Line( maxs, maxs.WithX( mins.x ), Color.Gray, 0.04f, false );
	}


	// [GameEvent.PreRender]
	[GameEvent.PreRender]
	public void DrawGrid()
	{
		/*var size = DeadLines.Manager.ArenaSize / 2;

		var gridSize = 128;
		var lines = ((size * 2f) / gridSize);
		var color = new Color( 0.13f );

		// Vertical
		var pos1 = new Vector3( -size, -size, 0f );
		var pos2 = new Vector3( -size, size, 0f );

		for ( var i = 1; i < lines; i++ )
		{
			pos1.x = -size + (i * gridSize);
			pos2.x = pos1.x;

			DebugOverlay.Line( pos1, pos2, color );
		}

		// Horizontal
		pos1 = new Vector3( -size, -size, 0f );
		pos2 = new Vector3( size, -size, 0f );

		for ( var i = 1; i < lines; i++ )
		{
			pos1.y = -size + (i * gridSize);
			pos2.y = pos1.y;

			DebugOverlay.Line( pos1, pos2, color );

		}*/
	}
}
