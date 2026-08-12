using api.Models;

namespace api.Services;

public static class GradeOrdering
{
    private static readonly Dictionary<GradeSystem, string[]> Ladders = new()
    {
        [GradeSystem.VScale] =
        [
            "VB", "V0", "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8",
            "V9", "V10", "V11", "V12", "V13", "V14", "V15", "V16", "V17"
        ],
        [GradeSystem.Yds] =
        [
            "5.0", "5.1", "5.2", "5.3", "5.4", "5.5", "5.6", "5.7", "5.8", "5.9",
            "5.10a", "5.10b", "5.10c", "5.10d",
            "5.11a", "5.11b", "5.11c", "5.11d",
            "5.12a", "5.12b", "5.12c", "5.12d",
            "5.13a", "5.13b", "5.13c", "5.13d",
            "5.14a", "5.14b", "5.14c", "5.14d",
            "5.15a", "5.15b", "5.15c", "5.15d"
        ],
        [GradeSystem.French] =
        [
            "1", "2", "3", "4", "4+", "5", "5+",
            "6a", "6a+", "6b", "6b+", "6c", "6c+",
            "7a", "7a+", "7b", "7b+", "7c", "7c+",
            "8a", "8a+", "8b", "8b+", "8c", "8c+",
            "9a", "9a+", "9b", "9b+", "9c"
        ],
        [GradeSystem.Font] =
        [
            "3", "4", "4+", "5", "5+",
            "6A", "6A+", "6B", "6B+", "6C", "6C+",
            "7A", "7A+", "7B", "7B+", "7C", "7C+",
            "8A", "8A+", "8B", "8B+", "8C", "8C+",
            "9A"
        ]
    };

    private static readonly Dictionary<GradeSystem, Dictionary<string, int>> Ranks = Ladders.ToDictionary(
        pair => pair.Key,
        pair => pair.Value
            .Select((grade, index) => (grade, index))
            .ToDictionary(entry => entry.grade, entry => entry.index, StringComparer.OrdinalIgnoreCase));

    public static int? Rank(GradeSystem system, string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade) || !Ranks.TryGetValue(system, out var ranks))
        {
            return null;
        }

        return ranks.TryGetValue(grade.Trim(), out var rank) ? rank : null;
    }
}
