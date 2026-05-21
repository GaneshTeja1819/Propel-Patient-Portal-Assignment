using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPACIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMergedClinicalEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add DeduplicationStatus column to patient_profiles_360 (US_027)
            migrationBuilder.AddColumn<string>(
                name: "DeduplicationStatus",
                table: "patient_profiles_360",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            // Create merged_clinical_entries table (US_027, AC-003, NFR-004)
            migrationBuilder.CreateTable(
                name: "merged_clinical_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncryptedLabel = table.Column<string>(type: "text", nullable: false),
                    EncryptedCanonicalValue = table.Column<string>(type: "text", nullable: false),
                    SourceDocumentIds = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    IsPhiField = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    MergedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merged_clinical_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_merged_clinical_entries_users_PatientId",
                        column: x => x.PatientId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create index on PatientId for efficient profile queries (AC-005, NFR-004)
            migrationBuilder.CreateIndex(
                name: "IX_MergedClinicalEntry_PatientId",
                table: "merged_clinical_entries",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "merged_clinical_entries");

            migrationBuilder.DropColumn(
                name: "DeduplicationStatus",
                table: "patient_profiles_360");
        }
    }
}
