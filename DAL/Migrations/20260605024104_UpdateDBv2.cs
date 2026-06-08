using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDBv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FleetWashLogs_BookingDetails_BookingDetailId",
                table: "FleetWashLogs");

            migrationBuilder.DropIndex(
                name: "IX_FleetWashLogs_BookingDetailId",
                table: "FleetWashLogs");

            migrationBuilder.DropColumn(
                name: "BookingDetailId",
                table: "FleetWashLogs");

            migrationBuilder.AddColumn<int>(
                name: "BookingId",
                table: "FleetWashLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "FleetWashLogs",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FleetWashLogs_BookingId",
                table: "FleetWashLogs",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_FleetWashLogs_Bookings_BookingId",
                table: "FleetWashLogs",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "BookingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FleetWashLogs_Bookings_BookingId",
                table: "FleetWashLogs");

            migrationBuilder.DropIndex(
                name: "IX_FleetWashLogs_BookingId",
                table: "FleetWashLogs");

            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "FleetWashLogs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FleetWashLogs");

            migrationBuilder.AddColumn<int>(
                name: "BookingDetailId",
                table: "FleetWashLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FleetWashLogs_BookingDetailId",
                table: "FleetWashLogs",
                column: "BookingDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_FleetWashLogs_BookingDetails_BookingDetailId",
                table: "FleetWashLogs",
                column: "BookingDetailId",
                principalTable: "BookingDetails",
                principalColumn: "DetailId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
