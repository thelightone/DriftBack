using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public sealed class SceneTransitionLoader : MonoBehaviour
{
    private const float AsyncSceneLoadProgressMax = 0.9f;

    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreenPrefab;
    [SerializeField] private Transform loadingScreenParent;
    [SerializeField] private float minimumLoadingScreenTime = 0.15f;

    private bool _isTransitionInProgress;

    public bool TryLoadScene(string sceneName)
    {
        if (_isTransitionInProgress)
        {
            Debug.LogWarning("Scene transition is already in progress.");
            return false;
        }

        StartCoroutine(LoadRegularSceneRoutine(NormalizeSceneReference(sceneName)));
        return true;
    }

    public bool TryLoadAddressableScene(string sceneAddress)
    {
        if (_isTransitionInProgress)
        {
            Debug.LogWarning("Scene transition is already in progress.");
            return false;
        }

        StartCoroutine(LoadAddressableSceneRoutine(NormalizeSceneReference(sceneAddress)));
        return true;
    }

    private IEnumerator LoadRegularSceneRoutine(string sceneName)
    {
        _isTransitionInProgress = true;
        Time.timeScale = 1f;

        GameObject loadingScreenInstance = InstantiateLoadingScreen();
        yield return null;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (loadOperation == null)
        {
            _isTransitionInProgress = false;
            Debug.LogError("Failed to create async scene load operation for scene: " + sceneName);
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        float elapsed = 0f;
        while (loadOperation.progress < AsyncSceneLoadProgressMax || elapsed < minimumLoadingScreenTime)
        {
            ApplyLoadingProgress(loadingScreenInstance, SceneLoadProgressNormalized(loadOperation));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ApplyLoadingProgress(loadingScreenInstance, 1f);
        loadOperation.allowSceneActivation = true;
    }

    private IEnumerator LoadAddressableSceneRoutine(string sceneAddress)
    {
        _isTransitionInProgress = true;
        Time.timeScale = 1f;

        GameObject loadingScreenInstance = InstantiateLoadingScreen();
        yield return null;

        AsyncOperationHandle<SceneInstance> loadHandle =
            Addressables.LoadSceneAsync(sceneAddress, LoadSceneMode.Single, activateOnLoad: false);

        float elapsed = 0f;
        while (!loadHandle.IsDone || elapsed < minimumLoadingScreenTime)
        {
            float p = loadHandle.PercentComplete;
            ApplyLoadingProgress(loadingScreenInstance, p);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (loadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Addressables.Release(loadHandle);

            if (loadingScreenInstance != null)
                Destroy(loadingScreenInstance);

            if (SceneLoader.TryLoadRaceSceneAfterAddressablesFail(sceneAddress))
            {
                _isTransitionInProgress = false;
                yield break;
            }

            _isTransitionInProgress = false;

            Debug.LogError(
                $"Addressables failed to load scene '{sceneAddress}'. " +
                "Verify that the scene is marked Addressable, that address matches exactly, and that remote bundles are rebuilt.\n" +
                loadHandle.OperationException);
            yield break;
        }

        ApplyLoadingProgress(loadingScreenInstance, 1f);
        AsyncOperation activateOperation = loadHandle.Result.ActivateAsync();
        while (!activateOperation.isDone)
            yield return null;
    }

    private GameObject InstantiateLoadingScreen()
    {
        if (loadingScreenPrefab == null)
            return null;

        GameObject loadingScreenInstance = loadingScreenParent != null
            ? Instantiate(loadingScreenPrefab, loadingScreenParent)
            : Instantiate(loadingScreenPrefab);

        loadingScreenInstance.SetActive(true);
        return loadingScreenInstance;
    }

    private static void ApplyLoadingProgress(GameObject loadingScreenRoot, float progress01)
    {
        if (loadingScreenRoot == null)
            return;

        LoadingPanelView view = loadingScreenRoot.GetComponent<LoadingPanelView>();
        if (view == null)
            view = loadingScreenRoot.GetComponentInChildren<LoadingPanelView>(true);

        view?.SetProgress01(progress01);
    }

    private static float SceneLoadProgressNormalized(AsyncOperation operation)
    {
        if (operation == null)
            return 0f;
        return Mathf.Clamp01(operation.progress / AsyncSceneLoadProgressMax);
    }

    private static string NormalizeSceneReference(string sceneReference)
    {
        return sceneReference.Replace("\\", "/").Trim();
    }
}
