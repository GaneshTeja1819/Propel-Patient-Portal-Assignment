namespace UPACIP.Application.Interfaces;

/// <summary>
/// Rule-based no-show risk scorer (AC-002).
/// Computes a 0–100 score from patient history and appointment lead time.
/// Implementations must be safe: any exception returns the default score of 0.
/// </summary>
public interface INoShowRiskScorer
{
    /// <summary>
    /// Scores no-show risk using: +10 per prior no-show, −1 per day lead time;
    /// result floored at 0 and capped at 100.
    /// </summary>
    int Score(int priorNoShowCount, int leadTimeDays);
}
