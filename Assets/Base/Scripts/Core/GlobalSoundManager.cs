using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GlobalSoundManager : MonoBehaviour
{
    private const string MutePrefKey = "global_sound_muted";

    private static GlobalSoundManager _instance;

    private bool _isMuted;
    private bool _isInitialized;

    public static bool IsMuted => EnsureInstance()._isMuted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void MuteAll()
    {
        SetMuted(true);
    }

    public static void UnmuteAll()
    {
        SetMuted(false);
    }

    public static void SetMuted(bool muted)
    {
        EnsureInstance().SetMutedInternal(muted);
    }

    private static GlobalSoundManager EnsureInstance()
    {
        if (_instance != null)
            return _instance;

        _instance = FindObjectOfType<GlobalSoundManager>(true);
        if (_instance == null)
        {
            GameObject managerObject = new GameObject("[GlobalSoundManager]");
            _instance = managerObject.AddComponent<GlobalSoundManager>();
        }

        _instance.InitializeIfNeeded();
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (_instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeIfNeeded()
    {
        if (_isInitialized)
            return;

        DontDestroyOnLoad(gameObject);
        _isMuted = PlayerPrefs.GetInt(MutePrefKey, 0) == 1;
        ApplyToAllAudioSources();
        _isInitialized = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToAllAudioSources();
    }

    private void SetMutedInternal(bool muted)
    {
        if (_isMuted != muted)
        {
            _isMuted = muted;
            PlayerPrefs.SetInt(MutePrefKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        ApplyToAllAudioSources();
    }

    private void ApplyToAllAudioSources()
    {
        AudioListener.pause = _isMuted;

        AudioSource[] audioSources = FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
                audioSources[i].mute = _isMuted;
        }
    }
}
