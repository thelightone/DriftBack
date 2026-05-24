using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SceneLoaderRaceContextTests
{
	[Test]
	public void PreparedTrainingRaceKeepsBackendRaceState ()
	{
		RaceSessionContext.BeginTrainingRace (
			"token",
			"season_1",
			"race_1",
			"seed_1",
			"player_1",
			"init_data",
			123,
			"https://example.test");

		var go = new GameObject ("scene-loader-test");
		try
		{
			var loader = go.AddComponent<SceneLoader> ();
			var prepareRaceContext = typeof (SceneLoader).GetMethod (
				"PrepareRaceContext",
				BindingFlags.Instance | BindingFlags.NonPublic);

			Assert.NotNull (prepareRaceContext);
			prepareRaceContext.Invoke (loader, new object[] { RaceMode.Training });

			Assert.That (RaceSessionContext.IsTraining, Is.True);
			Assert.That (RaceSessionContext.BackendRacePrepared, Is.True);
			Assert.That (RaceSessionContext.SeasonId, Is.EqualTo ("season_1"));
			Assert.That (RaceSessionContext.RaceId, Is.EqualTo ("race_1"));
			Assert.That (RaceSessionContext.Seed, Is.EqualTo ("seed_1"));
		}
		finally
		{
			Object.DestroyImmediate (go);
			RaceSessionContext.StartTraining ("", "", 0, "");
		}
	}
}
