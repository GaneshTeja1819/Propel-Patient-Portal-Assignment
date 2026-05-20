using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPACIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionCodingStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── verified_medical_codes: add decision, original_suggested_code ──
            // US_030 AC-002 (decision="Accepted"), AC-003 (decision="Modified" + original_suggested_code),
            // AC-004 (decision="Rejected"); AIR-005 human verification gate traceability.

            migrationBuilder.AddColumn<string>(
                name: "decision",
                table: "verified_medical_codes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "original_suggested_code",
                table: "verified_medical_codes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            // ── extracted_clinical_data: add coding_status ──
            // US_030 AC-005: set to "PendingManualCoding" when all suggestions are rejected;
            // set to "Complete" when at least one code is Accepted/Modified.

            migrationBuilder.AddColumn<string>(
                name: "coding_status",
                table: "extracted_clinical_data",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "coding_status",
                table: "extracted_clinical_data");

            migrationBuilder.DropColumn(
                name: "original_suggested_code",
                table: "verified_medical_codes");

            migrationBuilder.DropColumn(
                name: "decision",
                table: "verified_medical_codes");
        }
    }
}
