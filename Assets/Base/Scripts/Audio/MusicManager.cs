using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Background music across menu and race scenes. Loads <see cref="MusicSettings"/> from Resources ("MusicSettings").
/// Respects <see cref="GlobalSoundManager"/> mute (shared AudioSource.mute).
/// </summary>
public sealed class MusicManager : MonoBehaviour
{
    private const string ResourcesKey = "MusicSettings";

    private static MusicManager _instance;

    private MusicSettings _settings;
    private AudioSource _music;
    private bool _isInitialized;
    private string _lastAppliedSceneKey;
    private AudioClip _lastClip;
    private Coroutine _pendingRaceMusicRoutine;

    [Tooltip("Пауза перед стартом музыки на сцене гонки (сек). Меню без задержки.")]
    [SerializeField] private float raceMusicStartDelaySeconds = 2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapFirstScene()
    {
        EnsureInstance().ApplyForActiveScene();
    }

    private static MusicManager EnsureInstance()
    {
        if (_instance != null)
            return _instance;

        _instance = FindObjectOfType<MusicManager>(true);
        if (_instance == null)
        {
            var go = new GameObject("[MusicManager]");
            _instance = go.AddComponent<MusicManager>();
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
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopPendingRaceMusicRoutine();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene);
    }

    private void InitializeIfNeeded()
    {
        if (_isInitialized)
            return;

        DontDestroyOnLoad(gameObject);
        _settings = Resources.Load<MusicSettings>(ResourcesKey);
        if (_settings == null)
        {
            _isInitialized = true;
            return;
        }

        _music = gameObject.AddComponent<AudioSource>();
        _music.playOnAwake = false;
        _music.loop = true;
        _music.spatialBlend = 0f;
        _music.priority = 0;

        _isInitialized = true;
    }

    private void ApplyForActiveScene()
    {
        ApplyForScene(SceneManager.GetActiveScene());
    }

    private void ApplyForScene(Scene scene)
    {
        if (!_isInitialized)
            InitializeIfNeeded();

        if (_settings == null || _music == null)
            return;

        string sceneKey = scene.name + "\u001f" + scene.path;
        bool isMenu = _settings.IsMenuScene(scene);
        bool isRace = _settings.IsRaceScene(scene);

        AudioClip next = null;
        if (isMenu)
            next = _settings.MenuMusic;
        else if (isRace)
            next = _settings.RaceMusic;

        StopPendingRaceMusicRoutine();

        if (next == null)
        {
            _music.Stop();
            _music.clip = null;
            _lastClip = null;
            _lastAppliedSceneKey = sceneKey;
            return;
        }

        if (isMenu)
        {
            if (_lastAppliedSceneKey == sceneKey && _music.clip == next && _music.isPlaying)
                return;

            _lastAppliedSceneKey = sceneKey;
            ApplyMusicImmediate(next);
            return;
        }

        if (isRace)
        {
            _lastAppliedSceneKey = sceneKey;
            float delay = Mathf.Max(0f, raceMusicStartDelaySeconds);
            if (delay <= 0f)
            {
                ApplyMusicImmediate(next);
                return;
            }

            _music.Stop();
            _music.clip = null;
            _lastClip = null;
            _pendingRaceMusicRoutine = StartCoroutine(PlayRaceMusicAfterDelay(delay, sceneKey));
            return;
        }

        _lastAppliedSceneKey = sceneKey;
        _music.Stop();
        _music.clip = null;
        _lastClip = null;
    }

    private void StopPendingRaceMusicRoutine()
    {
        if (_pendingRaceMusicRoutine == null)
            return;

        StopCoroutine(_pendingRaceMusicRoutine);
        _pendingRaceMusicRoutine = null;
    }

    private void ApplyMusicImmediate(AudioClip clip)
    {
        if (clip == null || _music == null)
            return;

        _music.Stop();
        _music.clip = clip;
        _music.volume = _settings.Volume;
        _music.mute = GlobalSoundManager.IsMuted;
        _music.Play();
        _lastClip = clip;
    }

    private IEnumerator PlayRaceMusicAfterDelay(float delay, string expectedSceneKey)
    {
        yield return new WaitForSeconds(delay);
        _pendingRaceMusicRoutine = null;

        if (_settings == null || _music == null)
            yield break;

        Scene active = SceneManager.GetActiveScene();
        string activeKey = active.name + "\u001f" + active.path;
        if (activeKey != expectedSceneKey)
            yield break;

        if (!_settings.IsRaceScene(active))
            yield break;

        AudioClip clip = _settings.RaceMusic;
        if (clip == null)
            yield break;

        ApplyMusicImmediate(clip);
    }
}
