using System;
using System.Numerics;
using Sandbox;
using Sandbox.Services;

namespace DeadLines;


/// <summary>
/// Game Manager.
/// </summary>
public partial class DeadLines : Sandbox.GameManager
{
	public static int BestScore { get; set; } = 0;
	public static int BestCoopScore { get; set; } = 0;

	/// <summary>
	/// Has more than one player joined the game since last restart?
	/// </summary>
	[Net]
	public bool IsCoop { get; set; } = false;


	/// <summary>
	/// Determine what scoring bracket the current attempt should be.
	/// </summary>
	public static void UpdateScoringPlayerCount()
	{
		ScoringPlayerCount = Math.Max( ScoringPlayerCount, PlayerCount() );
		Manager.IsCoop = (ScoringPlayerCount > 1);
	}

	/// <summary>
	/// New game, new scoring bracket.
	/// </summary>
	public static void ResetScoringPlayerCount()
	{
		ScoringPlayerCount = PlayerCount();
		Manager.IsCoop = (ScoringPlayerCount > 1);
	}


	public static void UpdateBestScore()
	{
		BestScore = Cookie.Get( "BestScore", 0 );
		BestCoopScore = Cookie.Get( "BestScoreCoop", 0 );
	}

	public static void TrySetBestScore( int score, bool coop = false )
	{
		var k = coop ? "BestScoreCoop" : "BestScore";
		if ( Cookie.Get( k, 0 ) < score )
        {
			Cookie.Set( k, score );
            Log.Info("New record!");
        }

		UpdateBestScore();
	}

	[ClientRpc]
	public static void SubmitScores( int score )
	{
		Log.Info( "Submitted Score: " + score );
		Stats.SetValue( "hs_beta", score );

		TrySetBestScore( score, coop: false );
	}

	[ClientRpc]
	public static void SubmitCoopScores( int score )
	{
		Log.Info( "Submitted Coop Score: " + score );
		Stats.SetValue( "hs_coop_beta", score );

		TrySetBestScore( score, coop: true );
	}
}