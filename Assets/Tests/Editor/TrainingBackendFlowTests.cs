using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class TrainingBackendFlowTests
{
	[Test]
	public void TrainingStartUsesGlobalEndpointWithoutSeasonId()
	{
		Assert.That(
			BackendApi.BuildTrainingRaceStartUrl("https://api.example.test/"),
			Is.EqualTo("https://api.example.test/v1/training-races/start"));
	}

	[Test]
	public void BridgeMergePreservesPreparedTrainingRace()
	{
		RaceSessionContext.BeginTrainingRace(
			"access-token",
			"season_1",
			"race_1",
			"seed_1",
			"player_1",
			"init_1",
			123,
			"https://api.example.test",
			"track_desert");

		RaceSessionContext.MergeBridgeSnapshot(
			"player_2",
			"init_2",
			456,
			"https://api.changed.test");

		Assert.That(RaceSessionContext.IsTraining, Is.True);
		Assert.That(RaceSessionContext.BackendRacePrepared, Is.True);
		Assert.That(RaceSessionContext.AccessToken, Is.EqualTo("access-token"));
		Assert.That(RaceSessionContext.SeasonId, Is.EqualTo("season_1"));
		Assert.That(RaceSessionContext.RaceId, Is.EqualTo("race_1"));
		Assert.That(RaceSessionContext.Seed, Is.EqualTo("seed_1"));
		Assert.That(RaceSessionContext.MapId, Is.EqualTo("track_desert"));
		Assert.That(RaceSessionContext.PlayerId, Is.EqualTo("player_2"));
		Assert.That(RaceSessionContext.InitData, Is.EqualTo("init_2"));
		Assert.That(RaceSessionContext.TelegramUserId, Is.EqualTo(456));
		Assert.That(RaceSessionContext.BackendBaseUrl, Is.EqualTo("https://api.changed.test"));
	}
}

public static class TrainingBackendFlowSmokeRunner
{
	public static void Run()
	{
		try
		{
			var tests = new TrainingBackendFlowTests();
			tests.TrainingStartUsesGlobalEndpointWithoutSeasonId();
			tests.BridgeMergePreservesPreparedTrainingRace();
			Debug.Log("TrainingBackendFlow smoke checks passed.");
			EditorApplication.Exit(0);
		}
		catch (System.Exception ex)
		{
			Debug.LogException(ex);
			EditorApplication.Exit(1);
		}
	}
}
