using TMPro;
using UnityEngine;

public class LeaderboardPanelView : MonoBehaviour
{
    private const int TopCount = 10;

    [Header("Status")]
    public TMP_Text titleText;
    public TMP_Text statusText;

    [Header("Current player")]
    public TMP_Text playerRankText;
    public TMP_Text playerNameText;
    public TMP_Text playerScoreText;

    [Header("Top 10 — separate columns")]
    public TMP_Text[] usernameLines;
    public TMP_Text[] scoreLines;

    public void ShowLoading(string title = "Leaderboard")
    {
        if (titleText != null)
            titleText.text = title;

        if (statusText != null)
            statusText.text = "Loading…";

        SetCurrentPlayerSummary("…", "…", "…");
        ClearLines();
    }

    public void ShowError(string message)
    {
        if (statusText != null)
            statusText.text = message;

        SetCurrentPlayerSummary("—", "—", "—");
        ClearLines();
    }

    public void ShowEntries(string title, LeaderboardEntryDto[] entries, LeaderboardEntryDto currentPlayer)
    {
        if (titleText != null)
            titleText.text = title;

        if (statusText != null)
            statusText.text = "";

        ApplyCurrentPlayerSummary(currentPlayer);

        for (int i = 0; i < TopCount; i++)
        {
            var entry = (entries != null && i < entries.Length) ? entries[i] : null;
            SetLine(i, entry);
        }
    }

    private void SetLine(int index, LeaderboardEntryDto entry)
    {
        if (usernameLines != null && index < usernameLines.Length && usernameLines[index] != null)
            usernameLines[index].text = entry != null ? ResolveDisplayName(entry) : "—";

        if (scoreLines != null && index < scoreLines.Length && scoreLines[index] != null)
            scoreLines[index].text = entry != null ? entry.bestScore.ToString() : "—";
    }

    private void ClearLines()
    {
        if (usernameLines != null)
        {
            for (int i = 0; i < usernameLines.Length; i++)
            {
                if (usernameLines[i] != null)
                    usernameLines[i].text = "";
            }
        }

        if (scoreLines != null)
        {
            for (int i = 0; i < scoreLines.Length; i++)
            {
                if (scoreLines[i] != null)
                    scoreLines[i].text = "";
            }
        }
    }

    private void ApplyCurrentPlayerSummary(LeaderboardEntryDto currentPlayer)
    {
        if (currentPlayer == null || currentPlayer.rank <= 0)
        {
            SetCurrentPlayerSummary("—", "—", "—");
            return;
        }

        SetCurrentPlayerSummary(
            currentPlayer.rank.ToString(),
            ResolveDisplayName(currentPlayer),
            currentPlayer.bestScore.ToString());
    }

    private void SetCurrentPlayerSummary(string rank, string name, string score)
    {
        if (playerRankText != null)
            playerRankText.text = rank;

        if (playerNameText != null)
            playerNameText.text = name;

        if (playerScoreText != null)
            playerScoreText.text = score;
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
