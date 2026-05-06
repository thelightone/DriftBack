using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "MusicSettings", menuName = "DriftBack/Audio/Music Settings", order = 0)]
public sealed class MusicSettings : ScriptableObject
{
    [Header("Tracks")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip raceMusic;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.45f;

    [Header("Scene mapping")]
    [Tooltip("Menu music: each entry matches Scene.name exactly (e.g. InitScene), OR if it looks like a path (contains /, \\, ends with .unity, or starts with Assets/) — substring in Scene.path. Works for scenes loaded only via Addressables.")]
    [SerializeField] private string[] menuSceneNames = { "InitScene" };

    [Tooltip("Race music: leave empty = any scene that is not menu. Otherwise each entry uses the same rules as menu (name or path substring).")]
    [SerializeField] private string[] raceSceneNames;

    public AudioClip MenuMusic => menuMusic;
    public AudioClip RaceMusic => raceMusic;
    public float Volume => volume;
    public string[] MenuSceneNames => menuSceneNames;
    public string[] RaceSceneNames => raceSceneNames;

    public bool IsMenuScene(Scene scene)
    {
        return MatchesAnyRule(menuSceneNames, scene);
    }

    public bool IsRaceScene(Scene scene)
    {
        if (raceSceneNames == null || raceSceneNames.Length == 0)
            return !IsMenuScene(scene);

        return MatchesAnyRule(raceSceneNames, scene);
    }

    private static bool MatchesAnyRule(string[] rules, Scene scene)
    {
        if (rules == null)
            return false;

        string sceneName = scene.name;
        string scenePath = scene.path;

        for (int i = 0; i < rules.Length; i++)
        {
            if (PatternMatchesScene(rules[i], sceneName, scenePath))
                return true;
        }

        return false;
    }

    private static bool PatternMatchesScene(string pattern, string sceneName, string scenePath)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        string p = pattern.Trim();

        bool usePath =
            p.IndexOf('/') >= 0
            || p.IndexOf('\\') >= 0
            || p.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase);

        if (usePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return false;

            string normPath = scenePath.Replace('\\', '/');
            string normP = p.Replace('\\', '/');
            return normPath.IndexOf(normP, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        return string.Equals(p, sceneName, System.StringComparison.OrdinalIgnoreCase);
    }
}
