using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPACIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueueColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Queue display order — nullable int; null means not explicitly positioned (AC-001)
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "appointments",
                type: "integer",
                nullable: true);

            // Arrival timestamp — set when Staff marks patient as "Arrived" (AC-004)
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArrivedAt",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "ArrivedAt",
                table: "appointments");
        }
    }
}
