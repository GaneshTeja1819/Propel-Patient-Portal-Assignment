using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPACIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRankStatusAuditFieldsToMedicalCodeSuggestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── medical_code_suggestions: add rank, status, model_version, prompt_hash ──
            // US_029 AC-002 (rank), AC-004/AIR-006 (model_version, prompt_hash);
            // status drives the idempotency guard in CodeSuggestionJob (AC-001 edge case).

            migrationBuilder.AddColumn<int>(
                name: "rank",
                table: "medical_code_suggestions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "medical_code_suggestions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "model_version",
                table: "medical_code_suggestions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "prompt_hash",
                table: "medical_code_suggestions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCodeSuggestion_ClinicalDataId_Status",
                table: "medical_code_suggestions",
                columns: new[] { "ClinicalDataId", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicalCodeSuggestion_ClinicalDataId_Status",
                table: "medical_code_suggestions");

            migrationBuilder.DropColumn(
                name: "prompt_hash",
                table: "medical_code_suggestions");

            migrationBuilder.DropColumn(
                name: "model_version",
                table: "medical_code_suggestions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "medical_code_suggestions");

            migrationBuilder.DropColumn(
                name: "rank",
                table: "medical_code_suggestions");
        }
    }
}
