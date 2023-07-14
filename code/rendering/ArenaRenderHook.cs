using System;
using Sandbox;

namespace DeadLines;


public class ArenaRenderHook : RenderHook
{
	Material DotMaterial { get; set; } = Material.Load( "materials/sprites/circle.vmat" );


	public override void OnStage( SceneCamera target, Stage stage )
	{
		base.OnStage( target, stage );

		// Log.Info( "OnStage: " + stage.ToString() );

		if ( stage != Stage.AfterOpaque )
			return;

        target.Attributes.Clear();
        Graphics.Clear();

		Graphics.DrawQuad( new Rect( 0f, 0f, 8f, 8f ), DotMaterial, Color.White );



        // Grid Lines, but ignore this as DebugOverlay still overdraws


		var size = DeadLines.Manager.ArenaSize / 2;

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

			// DebugOverlay.Line( pos1, pos2, color );
		}

		// Horizontal
		pos1 = new Vector3( -size, -size, 0f );
		pos2 = new Vector3( size, -size, 0f );

		for ( var i = 1; i < lines; i++ )
		{
			pos1.y = -size + (i * gridSize);
			pos2.y = pos1.y;

			// DebugOverlay.Line( pos1, pos2, color );
		}
	}

	public override void OnFrame( SceneCamera target )
	{
		base.OnFrame( target );
	}
}