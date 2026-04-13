using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string raceSceneName = "Demo";
    [SerializeField] private string menuSceneName = "Demo";
    [SerializeField] private string backendBaseUrl = "https://your-backend-url.com";

    public void StartTrainingGame()
    {
        PrepareRaceContext(RaceMode.Training);
        LoadSceneByName(raceSceneName, "Race scene name is empty");
    }

    public void StartTournamentGame()
    {
        PrepareRaceContext(RaceMode.Tournament);
        LoadSceneByName(raceSceneName, "Race scene name is empty");
    }

    public void GoToMenu()
    {
        LoadSceneByName(menuSceneName, "Menu scene name is empty");
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void PrepareRaceContext(RaceMode mode)
    {
        var telegramBridge = new TelegramBridge();
        var cached = LocalProfileCache.Load();
        var telegramUser = telegramBridge.GetUser();

        string playerId = cached.playerId;
        string initData = telegramBridge.GetInitData();
        long telegramUserId = telegramUser != null ? telegramUser.id : 0;

        if (mode == RaceMode.Tournament)
        {
            RaceSessionContext.StartTournament(
                playerId,
                initData,
                telegramUserId,
                backendBaseUrl
            );
        }
        else
        {
            RaceSessionContext.StartTraining(
                playerId,
                initData,
                telegramUserId,
                backendBaseUrl
            );
        }

        Debug.Log(
            $"Race context prepared. Mode={RaceSessionContext.CurrentMode}, PlayerId={playerId}, TelegramUserId={telegramUserId}");
    }

    private async void LoadSceneByName(string sceneName, string emptySceneMessage)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError(emptySceneMessage);
            return;
        }

        Time.timeScale = 1f;
        string normalizedSceneName = sceneName.Replace("\\", "/").Trim();
        Debug.Log(
            $"Trying to load scene via Addressables '{normalizedSceneName}', mode: {RaceSessionContext.CurrentMode}");

        try
        {
            await Addressables.LoadSceneAsync(normalizedSceneName).Task;
            Debug.Log("Scene loaded via Addressables: " + normalizedSceneName);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"Addressables failed to load scene '{normalizedSceneName}'. " +
                "Verify that the scene is marked Addressable and that address matches exactly.\n" + ex);
        }
    }
}