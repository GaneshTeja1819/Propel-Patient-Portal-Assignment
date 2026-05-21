using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPACIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalkInBookingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing NOT NULL FK on PatientId before altering nullability
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_users_PatientId",
                table: "appointments");

            // Make PatientId nullable to support anonymous walk-in bookings (AC-002)
            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                table: "appointments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // Staff actor attribution column (AC-001, AC-004)
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByStaffId",
                table: "appointments",
                type: "uuid",
                nullable: true);

            // Anonymous patient details stored as JSONB (AC-002)
            migrationBuilder.AddColumn<string>(
                name: "AnonymousPatientDetails",
                table: "appointments",
                type: "jsonb",
                nullable: true);

            // Re-add FK allowing nulls (anonymous appointments have no linked User)
            migrationBuilder.AddForeignKey(
                name: "FK_appointments_users_PatientId",
                table: "appointments",
                column: "PatientId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_users_PatientId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "CreatedByStaffId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "AnonymousPatientDetails",
                table: "appointments");

            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                table: "appointments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_users_PatientId",
                table: "appointments",
                column: "PatientId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
