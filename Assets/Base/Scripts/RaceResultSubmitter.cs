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

        if (RaceSessionContext.IsTournament)
        {
            Debug.Log($"Tournament finished. Result submit is temporarily disabled. Score={score}, Time={timeSeconds}");
        }
        else
        {
            Debug.Log($"Training finished. Result submit is temporarily disabled. Score={score}, Time={timeSeconds}");
        }
    }
}