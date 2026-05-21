namespace UPACIP.Application.Commands.Codes;

/// <summary>
/// Command for <c>POST /api/v1/codes/verify</c> (US_030, AC-001).
///
/// Carries the staff actor identity, the targeted suggestion, and the
/// verification decision. <see cref="VerifiedCode"/> is required only
/// when <see cref="Decision"/> is <c>"Modified"</c> (AC-003).
/// </summary>
/// <param name="SuggestionId">ID of the <c>MedicalCodeSuggestion</c> being verified.</param>
/// <param name="Decision">Staff decision: <c>"Accepted"</c>, <c>"Modified"</c>, or <c>"Rejected"</c>.</param>
/// <param name="VerifiedCode">
/// The staff-chosen code when <see cref="Decision"/> is <c>"Modified"</c>;
/// null for Accepted and Rejected paths.
/// </param>
/// <param name="ActorStaffId">Staff user ID extracted from the JWT <c>sub</c> claim.</param>
public sealed record VerifyCodeCommand(
    Guid SuggestionId,
    string Decision,
    string? VerifiedCode,
    Guid ActorStaffId);

/// <summary>Result returned by <see cref="Handlers.Codes.VerifyCodeHandler"/>.</summary>
/// <param name="VerifiedMedicalCodeId">ID of the newly created <c>VerifiedMedicalCode</c> record.</param>
/// <param name="CodingStatusUpdated">
/// True when <c>ExtractedClinicalData.CodingStatus</c> was changed
/// (either to <c>"PendingManualCoding"</c> or <c>"Complete"</c>).
/// </param>
public sealed record VerifyCodeResult(
    Guid VerifiedMedicalCodeId,
    bool CodingStatusUpdated);
