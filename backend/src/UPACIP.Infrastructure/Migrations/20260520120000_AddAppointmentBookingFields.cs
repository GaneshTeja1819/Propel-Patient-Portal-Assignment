using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPACIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentBookingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NoShowRiskScore",
                table: "appointments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceValidationStatus",
                table: "appointments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "NotProvided");

            migrationBuilder.AddColumn<string>(
                name: "InsuranceProvider",
                table: "appointments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceId",
                table: "appointments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "NoShowRiskScore",           table: "appointments");
            migrationBuilder.DropColumn(name: "InsuranceValidationStatus", table: "appointments");
            migrationBuilder.DropColumn(name: "InsuranceProvider",         table: "appointments");
            migrationBuilder.DropColumn(name: "InsuranceId",               table: "appointments");
        }
    }
}
