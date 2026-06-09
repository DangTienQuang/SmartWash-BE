using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDBv3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FleetWashLogs_Bookings_BookingId",
                table: "FleetWashLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FleetWashLogs_Branches_BranchId",
                table: "FleetWashLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FleetWashLogs_FleetVehicles_FleetVehicleId",
                table: "FleetWashLogs");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "FleetWashLogs",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_FleetWashLogs_Bookings_BookingId",
                table: "FleetWashLogs",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "BookingId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FleetWashLogs_Branches_BranchId",
                table: "FleetWashLogs",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FleetWashLogs_FleetVehicles_FleetVehicleId",
                table: "FleetWashLogs",
                column: "FleetVehicleId",
                principalTable: "FleetVehicles",
                principalColumn: "FleetVehicleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FleetWashLogs_Bookings_BookingId",
                table: "FleetWashLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FleetWashLogs_Branches_BranchId",
                table: "FleetWashLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FleetWashLogs_FleetVehicles_FleetVehicleId",
                table: "FleetWashLogs");

            migrationBuilder.UpdateData(
                table: "FleetWashLogs",
                keyColumn: "Status",
                keyValue: null,
                column: "Status",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "FleetWashLogs",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_FleetWashLogs_Bookings_BookingId",
                table: "FleetWashLogs",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_FleetWashLogs_Branches_BranchId",
                table: "FleetWashLogs",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FleetWashLogs_FleetVehicles_FleetVehicleId",
                table: "FleetWashLogs",
                column: "FleetVehicleId",
                principalTable: "FleetVehicles",
                principalColumn: "FleetVehicleId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
