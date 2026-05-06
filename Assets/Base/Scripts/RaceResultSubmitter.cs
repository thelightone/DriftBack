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
        int safeScore = Mathf.Max(0, score);
        float safeTimeSeconds = Mathf.Max(0f, timeSeconds);
        int safeRaceCoinsEarned = Mathf.Clamp(raceCoinsEarned, 1, 20);

        if (RaceSessionContext.IsTournament && RaceSessionContext.BackendRacePrepared &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.AccessToken) &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.SeasonId) &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.RaceId) &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.Seed))
        {
            StartCoroutine(SubmitTournamentFinish(safeScore));
            return;
        }

        if (RaceSessionContext.IsTraining && RaceSessionContext.BackendRacePrepared &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.AccessToken) &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.SeasonId) &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.RaceId) &&
            !string.IsNullOrWhiteSpace(RaceSessionContext.Seed))
        {
            StartCoroutine(SubmitTrainingFinish(safeTimeSeconds, safeScore, safeRaceCoinsEarned));
            return;
        }

        Debug.Log(
            $"Race finished (no backend submit). Mode training={RaceSessionContext.IsTraining}, tournament={RaceSessionContext.IsTournament}, prepared={RaceSessionContext.BackendRacePrepared}. Score={score}, Time={timeSeconds}, RaceCoins={raceCoinsEarned}");
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

    private IEnumerator SubmitTrainingFinish(float timeSeconds, int score, int raceCoinsEarned)
    {
        var api = new BackendApi(RaceSessionContext.BackendBaseUrl);
        var body = new TrainingRaceFinishRequest
        {
            raceId = RaceSessionContext.RaceId,
            seed = RaceSessionContext.Seed,
            score = score,
            timeSeconds = timeSeconds,
            raceCoinsEarned = raceCoinsEarned
        };

        TrainingRaceFinishResponse response = null;
        string err = null;
        yield return api.FinishTrainingRace(
            RaceSessionContext.AccessToken,
            RaceSessionContext.SeasonId,
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
                $"FinishTrainingRace ok. score={response.score}, bestScore={response.bestScore}, isNewBest={response.isNewBest}, raceCoinsEarned={response.raceCoinsEarned}, raceCoinsBalance={response.raceCoinsBalance}");
    }
}
