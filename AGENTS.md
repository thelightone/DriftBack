# Agent Context: DriftBack Unity Client

This repo is the Unity/WebGL client for the RACEDRIFT Telegram Mini App. It talks to the backend in `/Users/ivan/ai-powered/cars-racing`.

## Training Flow

- Free training must work even when there is no active tournament season.
- Training start uses `POST /v1/training-races/start` without a season id.
- The training start response contains `raceId`, `seed`, `seasonId`, and `mapId`.
- `AppManager.StartTrainingRaceFlow()` must use the `seasonId` from that start response for finish submission.
- Do not resolve training through `ResolveActiveSeasonIdForTournament()`; that helper is for ranked tournament flow and requires `status == "active"`.
- `RaceSessionContext.BeginTrainingRace(...)` stores backend race context before scene load.
- `SceneLoader.PrepareRaceContext(RaceMode.Training)` must preserve an already prepared backend race by calling `MergeBridgeSnapshot(...)`; calling `StartTraining(...)` there clears `raceId`, `seed`, and `seasonId`.
- Training finish still uses `POST /v1/seasons/{seasonId}/training-races/finish`.

## Verification

Preferred quick check for the training backend flow:

```bash
"/Applications/Unity/Hub/Editor/6000.2.10f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit \
  -projectPath /Users/ivan/ai-powered/DriftBack \
  -executeMethod TrainingBackendFlowSmokeRunner.Run \
  -logFile /tmp/driftback-training-smoke.log
```

After running Unity in batch mode, check `git status`: Unity may touch generated/imported assets. Do not commit unrelated generated churn.
