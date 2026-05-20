using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPACIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendDataConflict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── data_conflicts: add new columns (US_028, AC-001, AC-002) ──────────

            // Status replaces the boolean is_resolved column.
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "data_conflicts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "data_conflicts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Medium");

            // JSON array of competing source values; replaces SourceValue / TargetValue.
            migrationBuilder.AddColumn<string>(
                name: "ConflictingValues",
                table: "data_conflicts",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "CanonicalValue",
                table: "data_conflicts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedById",
                table: "data_conflicts",
                type: "uuid",
                nullable: true);

            // Migrate existing boolean: IsResolved=true → Status="Resolved"
            migrationBuilder.Sql(
                "UPDATE data_conflicts SET \"Status\" = 'Resolved' WHERE \"IsResolved\" = TRUE;");

            // Drop legacy boolean column after migrating its data.
            migrationBuilder.DropColumn(name: "IsResolved",  table: "data_conflicts");
            migrationBuilder.DropColumn(name: "SourceValue", table: "data_conflicts");
            migrationBuilder.DropColumn(name: "TargetValue", table: "data_conflicts");

            // Composite index for efficient status-filtered queries per patient (AC-001, NFR).
            migrationBuilder.CreateIndex(
                name: "IX_DataConflict_PatientId_Status",
                table: "data_conflicts",
                columns: new[] { "PatientId", "Status" });

            // ── users: add LastConflictReviewedAt for isNew computation ───────────
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastConflictReviewedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataConflict_PatientId_Status",
                table: "data_conflicts");

            migrationBuilder.DropColumn(name: "Status",           table: "data_conflicts");
            migrationBuilder.DropColumn(name: "Severity",         table: "data_conflicts");
            migrationBuilder.DropColumn(name: "ConflictingValues", table: "data_conflicts");
            migrationBuilder.DropColumn(name: "CanonicalValue",   table: "data_conflicts");
            migrationBuilder.DropColumn(name: "ResolvedById",     table: "data_conflicts");

            // Restore legacy columns (empty values; data is not recoverable after migration).
            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "data_conflicts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceValue",
                table: "data_conflicts",
                type: "text",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "TargetValue",
                table: "data_conflicts",
                type: "text",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.DropColumn(
                name: "LastConflictReviewedAt",
                table: "users");
        }
    }
}
