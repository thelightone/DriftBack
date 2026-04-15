using System.Text.RegularExpressions;

public static class SeasonDetailBestScoreParser
{
    public static string FormatHighScoreDisplay(string json)
    {
        if (string.IsNullOrEmpty(json))
            return "—";

        if (Regex.IsMatch(json, "\"bestScore\"\\s*:\\s*null"))
            return "—";

        Match m = Regex.Match(json, "\"bestScore\"\\s*:\\s*(-?\\d+)");
        if (m.Success)
            return m.Groups[1].Value;

        return "—";
    }
}
