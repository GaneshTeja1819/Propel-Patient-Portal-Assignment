using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPACIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalDocumentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_medical_code_suggestions_ClinicalDataId",
                table: "medical_code_suggestions");

            migrationBuilder.DropIndex(
                name: "IX_data_conflicts_PatientId",
                table: "data_conflicts");

            migrationBuilder.DropIndex(
                name: "IX_clinical_documents_PatientId",
                table: "clinical_documents");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "data_conflicts");

            migrationBuilder.DropColumn(
                name: "SourceValue",
                table: "data_conflicts");

            migrationBuilder.DropColumn(
                name: "TargetValue",
                table: "data_conflicts");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastConflictReviewedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

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
                name: "CanonicalValue",
                table: "data_conflicts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConflictingValues",
                table: "data_conflicts",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedById",
                table: "data_conflicts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "data_conflicts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Medium");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "data_conflicts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "data_conflicts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "ExtractionFailureNote",
                table: "clinical_documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractionStatus",
                table: "clinical_documents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "clinical_documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCodeSuggestion_ClinicalDataId_Status",
                table: "medical_code_suggestions",
                columns: new[] { "ClinicalDataId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_DataConflict_PatientId_Status",
                table: "data_conflicts",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_clinical_documents_PatientId_FileHash",
                table: "clinical_documents",
                columns: new[] { "PatientId", "FileHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicalCodeSuggestion_ClinicalDataId_Status",
                table: "medical_code_suggestions");

            migrationBuilder.DropIndex(
                name: "IX_DataConflict_PatientId_Status",
                table: "data_conflicts");

            migrationBuilder.DropIndex(
                name: "IX_clinical_documents_PatientId_FileHash",
                table: "clinical_documents");

            migrationBuilder.DropColumn(
                name: "LastConflictReviewedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "model_version",
                table: "medical_code_suggestions");

            migrationBuilder.DropColumn(
                name: "prompt_hash",
                table: "medical_code_suggestions");

            migrationBuilder.DropColumn(
                name: "rank",
                table: "medical_code_suggestions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "medical_code_suggestions");

            migrationBuilder.DropColumn(
                name: "CanonicalValue",
                table: "data_conflicts");

            migrationBuilder.DropColumn(
                name: "ConflictingValues",
                table: "data_conflicts");

            migrationBuilder.DropColumn(
                name: "ResolvedById",
                table: "data_conflicts");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "data_conflicts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "data_conflicts");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "data_conflicts");

            migrationBuilder.DropColumn(
                name: "ExtractionFailureNote",
                table: "clinical_documents");

            migrationBuilder.DropColumn(
                name: "ExtractionStatus",
                table: "clinical_documents");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "clinical_documents");

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
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetValue",
                table: "data_conflicts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_medical_code_suggestions_ClinicalDataId",
                table: "medical_code_suggestions",
                column: "ClinicalDataId");

            migrationBuilder.CreateIndex(
                name: "IX_data_conflicts_PatientId",
                table: "data_conflicts",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_clinical_documents_PatientId",
                table: "clinical_documents",
                column: "PatientId");
        }
    }
}
