using System;
using System.ComponentModel;
using Sandbox;

namespace DeadLines;


public partial class CenterDot : ModelEntity
{
	public Color Color { get; set; } = Color.Gray;


	public override void Spawn()
	{
		SetModel( "models/vector/circle_filled.vmdl" );
		RenderColor = Color;
        Scale = 0.2f;

		Position = Vector3.Down * 100f;

		EnableTraceAndQueries = false;
		EnableAllCollisions = false;
	}

}