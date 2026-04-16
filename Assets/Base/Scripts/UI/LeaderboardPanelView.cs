using TMPro;
using UnityEngine;

public class LeaderboardPanelView : MonoBehaviour
{
    [Header("Status")]
    public TMP_Text titleText;
    public TMP_Text statusText;

    [Header("Top 10 lines")]
    [Tooltip("Assign 10 TMP_Text elements (1..10). Each line will be formatted like: \"1. @username — 123\".")]
    public TMP_Text[] entryLines;

    public void ShowLoading(string title = "Leaderboard")
    {
        if (titleText != null)
            titleText.text = title;

        if (statusText != null)
            statusText.text = "Loading…";

        ClearLines();
    }

    public void ShowError(string message)
    {
        if (statusText != null)
            statusText.text = message;

        ClearLines();
    }

    public void ShowEntries(string title, LeaderboardEntryDto[] entries)
    {
        if (titleText != null)
            titleText.text = title;

        if (statusText != null)
            statusText.text = "";

        if (entryLines == null || entryLines.Length == 0)
            return;

        for (int i = 0; i < entryLines.Length; i++)
        {
            var line = entryLines[i];
            if (line == null)
                continue;

            var entry = (entries != null && i < entries.Length) ? entries[i] : null;
            line.text = entry != null
                ? FormatEntry(entry)
                : (i + 1) + ". —";
        }
    }

    private void ClearLines()
    {
        if (entryLines == null)
            return;

        for (int i = 0; i < entryLines.Length; i++)
        {
            if (entryLines[i] != null)
                entryLines[i].text = "";
        }
    }

    private string FormatEntry(LeaderboardEntryDto e)
    {
        string name = ResolveDisplayName(e);
        string rank = e.rank > 0 ? e.rank.ToString() : "?";
        return rank + ". " + name + " — " + e.bestScore;
    }

    private string ResolveDisplayName(LeaderboardEntryDto e)
    {
        if (e == null)
            return "—";

        if (!string.IsNullOrWhiteSpace(e.username))
            return "@" + e.username.Trim();

        if (!string.IsNullOrWhiteSpace(e.firstName))
            return e.firstName.Trim();

        return "Player";
    }
}

