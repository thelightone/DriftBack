using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class SceneLoader : MonoBehaviour
{
    private static string s_lastLoadedRaceSceneAddress;
    private static bool s_isAddressableLoadInProgress;

    [SerializeField] private string raceSceneName = "Demo";
    [SerializeField] private string menuSceneName = "Demo";
    [SerializeField] private string backendBaseUrl = "https://your-backend-url.com";
    [SerializeField] private SceneTransitionLoader sceneTransitionLoader;

    public void StartTrainingGame()
    {
        PrepareRaceContext(RaceMode.Training);
        LoadRaceScene();
    }

    public void StartTournamentGame()
    {
        PrepareRaceContext(RaceMode.Tournament);
        LoadRaceScene();
    }

    public void GoToMenu()
    {
        LoadScene(menuSceneName, sceneTransitionLoader);
    }

    public void RestartScene()
    {
        RestartRaceScene(sceneTransitionLoader);
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
            if (RaceSessionContext.BackendRacePrepared)
                RaceSessionContext.MergeBridgeSnapshot(playerId, initData, telegramUserId, backendBaseUrl);
            else
                RaceSessionContext.StartTournament(playerId, initData, telegramUserId, backendBaseUrl);
        }
        else
        {
            RaceSessionContext.StartTraining(playerId, initData, telegramUserId, backendBaseUrl);
        }

        Debug.Log(
            $"Race context prepared. Mode={RaceSessionContext.CurrentMode}, PlayerId={playerId}, TelegramUserId={telegramUserId}, BackendRacePrepared={RaceSessionContext.BackendRacePrepared}");
    }

    public static void RestartRaceScene(SceneTransitionLoader transitionLoader = null)
    {
        string raceSceneAddress = !string.IsNullOrWhiteSpace(s_lastLoadedRaceSceneAddress)
            ? s_lastLoadedRaceSceneAddress
            : SceneManager.GetActiveScene().name;

        LoadAddressableScene(
            raceSceneAddress,
            "Race scene address is empty",
            cacheAsRaceScene: true,
            transitionLoader
        );
    }

    public static void LoadScene(string sceneName, SceneTransitionLoader transitionLoader = null)
    {
        LoadRegularScene(sceneName, "Scene name is empty", transitionLoader);
    }

    private void LoadRaceScene()
    {
        LoadAddressableScene(
            raceSceneName,
            "Race scene name is empty",
            cacheAsRaceScene: true,
            sceneTransitionLoader
        );
    }

    private static void LoadRegularScene(
        string sceneName,
        string emptySceneMessage,
        SceneTransitionLoader transitionLoader
    )
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(emptySceneMessage);
            return;
        }

        string normalizedSceneName = NormalizeSceneReference(sceneName);
        Time.timeScale = 1f;

        if (transitionLoader != null)
        {
            if (!transitionLoader.TryLoadScene(normalizedSceneName))
                Debug.LogWarning("Regular scene load was skipped because another transition is already in progress.");

            return;
        }

        SceneManager.LoadScene(normalizedSceneName);
    }

    private static void LoadAddressableScene(
        string sceneAddress,
        string emptySceneMessage,
        bool cacheAsRaceScene,
        SceneTransitionLoader transitionLoader
    )
    {
        if (string.IsNullOrWhiteSpace(sceneAddress))
        {
            Debug.LogError(emptySceneMessage);
            return;
        }

        if (s_isAddressableLoadInProgress)
        {
            Debug.LogWarning("Scene load is already in progress.");
            return;
        }

        string normalizedSceneName = NormalizeSceneReference(sceneAddress);
        if (cacheAsRaceScene)
            s_lastLoadedRaceSceneAddress = normalizedSceneName;

        Time.timeScale = 1f;
        Debug.Log(
            $"Trying to load scene via Addressables '{normalizedSceneName}', mode: {RaceSessionContext.CurrentMode}");

        if (transitionLoader != null)
        {
            if (!transitionLoader.TryLoadAddressableScene(normalizedSceneName))
                Debug.LogWarning("Addressable scene load was skipped because another transition is already in progress.");

            return;
        }

        s_isAddressableLoadInProgress = true;
        AsyncOperationHandle<SceneInstance> handle =
            Addressables.LoadSceneAsync(normalizedSceneName, LoadSceneMode.Single);

        handle.Completed += completedHandle =>
        {
            s_isAddressableLoadInProgress = false;

            if (completedHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Scene loaded via Addressables: " + normalizedSceneName);
                return;
            }

            Addressables.Release(completedHandle);

            if (TryLoadRaceSceneAfterAddressablesFail(normalizedSceneName))
                return;

            Debug.LogError(
                $"Addressables failed to load scene '{normalizedSceneName}'. " +
                "Verify that the scene is marked Addressable, that address matches exactly, and that remote bundles are rebuilt.\n" +
                completedHandle.OperationException);
        };
    }

    /// <summary>
    /// When Addressables cannot load the race scene (common in Editor if catalog or bundles are out of date),
    /// try Editor direct open, then scenes registered in Build Settings.
    /// </summary>
    public static bool TryLoadRaceSceneAfterAddressablesFail(string normalizedSceneReference)
    {
        if (string.IsNullOrWhiteSpace(normalizedSceneReference))
            return false;

        normalizedSceneReference = NormalizeSceneReference(normalizedSceneReference);

#if UNITY_EDITOR
        if (normalizedSceneReference.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
            normalizedSceneReference.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                EditorSceneManager.OpenScene(normalizedSceneReference, OpenSceneMode.Single);
                Debug.LogWarning(
                    $"Addressables failed; opened scene in Editor via path: {normalizedSceneReference}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Editor OpenScene fallback failed: {ex.Message}");
            }
        }
#endif

        if (normalizedSceneReference.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(normalizedSceneReference);
            if (buildIndex >= 0)
            {
                Debug.LogWarning(
                    $"Addressables failed; loading scene from Build Settings by path (index {buildIndex}).");
                SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
                return true;
            }
        }

        string shortName = GetSceneAssetNameWithoutExtension(normalizedSceneReference);
        if (!string.IsNullOrEmpty(shortName) && IsSceneInBuildSettings(shortName))
        {
            Debug.LogWarning($"Addressables failed; loading scene from Build Settings by name: {shortName}");
            SceneManager.LoadScene(shortName, LoadSceneMode.Single);
            return true;
        }

        return false;
    }

    private static string GetSceneAssetNameWithoutExtension(string normalizedReference)
    {
        string s = normalizedReference.Trim();
        if (s.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            s = s.Substring(0, s.Length - ".unity".Length);

        int i = s.LastIndexOf('/');
        return i >= 0 ? s.Substring(i + 1) : s;
    }

    private static bool IsSceneInBuildSettings(string sceneNameWithoutPath)
    {
        if (string.IsNullOrEmpty(sceneNameWithoutPath))
            return false;

        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = GetSceneAssetNameWithoutExtension(path);
            if (string.Equals(name, sceneNameWithoutPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string NormalizeSceneReference(string sceneReference)
    {
        return sceneReference.Replace("\\", "/").Trim();
    }
}