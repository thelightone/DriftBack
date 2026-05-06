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

    private void OnRaceFinished(float timeSeconds, int score, int raceCoinsEarned)
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

        if (RaceSessionContext.IsTraining &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.AccessToken) &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.BackendBaseUrl))
        {
            StartCoroutine(SubmitTrainingFinish(timeSeconds, raceCoinsEarned));
            return;
        }

        Debug.Log(
            $"Race finished (no backend submit). Mode tournament={RaceSessionContext.IsTournament}, prepared={RaceSessionContext.BackendRacePrepared}. Score={score}, Time={timeSeconds}, TrainingRC={raceCoinsEarned}");
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

    private IEnumerator SubmitTrainingFinish(float timeSeconds, int coinsEarned)
    {
        var api = new BackendApi(RaceSessionContext.BackendBaseUrl);
        var body = new TrainingRaceFinishRequest
        {
            lapTimeSeconds = timeSeconds,
            coinsEarned = Mathf.Max(0, coinsEarned)
        };

        TrainingRaceFinishResponse response = null;
        string err = null;
        yield return api.FinishTrainingRace(
            RaceSessionContext.AccessToken,
            body,
            r => response = r,
            e => err = e);

        if (!string.IsNullOrEmpty(err))
        {
            Debug.LogError("FinishTrainingRace failed: " + err);
            yield break;
        }

        if (response != null)
            Debug.Log(
                $"FinishTrainingRace ok. coinsGranted={response.coinsGranted}, raceCoinsBalance={response.raceCoinsBalance}");
    }
}
