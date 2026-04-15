using System.Collections;
using UnityEngine;

public class RaceResultSubmitter : MonoBehaviour
{
    private bool _submitted;

    private void OnEnable()
    {
        RaceFlowManager.RaceFinished += OnRaceFinished;
    }

    private void OnDisable()
    {
        RaceFlowManager.RaceFinished -= OnRaceFinished;
    }

    private void OnRaceFinished(float timeSeconds, int score)
    {
        if (_submitted)
            return;

        _submitted = true;

        if (RaceSessionContext.IsTournament && RaceSessionContext.BackendRacePrepared &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.AccessToken) &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.SeasonId) &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.RaceId) &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.Seed))
        {
            int safeScore = Mathf.Max(0, score);
            StartCoroutine(SubmitTournamentFinish(safeScore));
            return;
        }

        Debug.Log(
            $"Race finished (no backend submit). Mode tournament={RaceSessionContext.IsTournament}, prepared={RaceSessionContext.BackendRacePrepared}. Score={score}, Time={timeSeconds}");
    }

    private IEnumerator SubmitTournamentFinish(int score)
    {
        var api = new BackendApi(RaceSessionContext.BackendBaseUrl);
        var body = new SeasonRaceFinishRequest
        {
            raceId = RaceSessionContext.RaceId,
            seed = RaceSessionContext.Seed,
            score = score
        };

        SeasonRaceFinishResponse response = null;
        string err = null;
        yield return api.FinishSeasonRace(
            RaceSessionContext.AccessToken,
            RaceSessionContext.SeasonId,
            body,
            r => response = r,
            e => err = e);

        if (!string.IsNullOrEmpty(err))
        {
            Debug.LogError("FinishSeasonRace failed: " + err);
            yield break;
        }

        if (response != null)
            Debug.Log(
                $"FinishSeasonRace ok. score={response.score}, bestScore={response.bestScore}, isNewBest={response.isNewBest}");
    }
}
