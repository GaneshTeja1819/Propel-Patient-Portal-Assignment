using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Services;

/// <summary>
/// Rule-based no-show risk scorer (AC-002).
///
/// Scoring formula:
///   score = (priorNoShowCount × 10) − leadTimeDays
///   Clamped to the range [0, 100].
///
/// Examples:
///   0 prior no-shows, 7 days lead  → max(0, 0 − 7)  = 0
///   2 prior no-shows, 3 days lead  → max(0, 20 − 3)  = 17
///   5 prior no-shows, 0 days lead  → min(100, 50 − 0) = 50
/// </summary>
public sealed class NoShowRiskScorer : INoShowRiskScorer
{
    private const int NoShowPenalty = 10;
    private const int MinScore = 0;
    private const int MaxScore = 100;

    public int Score(int priorNoShowCount, int leadTimeDays)
    {
        var raw = (priorNoShowCount * NoShowPenalty) - leadTimeDays;
        return Math.Clamp(raw, MinScore, MaxScore);
    }
}
