using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetVehicleToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetails_Invoices_InvoiceId",
                table: "BookingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetails_Lanes_ProcessingLaneId",
                table: "BookingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetails_Users_ProcessingStaffId",
                table: "BookingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetails_VehicleTypes_ActualVehicleTypeId",
                table: "BookingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetails_Vehicles_VehicleId",
                table: "BookingDetails");

            migrationBuilder.DropIndex(
                name: "IX_BookingDetails_ActualVehicleTypeId",
                table: "BookingDetails");

            migrationBuilder.DropIndex(
                name: "IX_BookingDetails_InvoiceId",
                table: "BookingDetails");

            migrationBuilder.DropIndex(
                name: "IX_BookingDetails_ProcessingLaneId",
                table: "BookingDetails");

            migrationBuilder.DropIndex(
                name: "IX_BookingDetails_ProcessingStaffId",
                table: "BookingDetails");

            migrationBuilder.DropIndex(
                name: "IX_BookingDetails_VehicleId",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "ActualPrice",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "ActualVehicleTypeId",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "CapacityWeight",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "DepositStatus",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "MismatchSurcharge",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "ProcessingLaneId",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "ProcessingStaffId",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "VehicleCondition",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "BookingDetails");

            migrationBuilder.AddColumn<int>(
                name: "FleetVehicleId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FleetWashLogs",
                columns: table => new
                {
                    FleetWashLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FleetVehicleId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    BookingDetailId = table.Column<int>(type: "int", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WashCost = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetWashLogs", x => x.FleetWashLogId);
                    table.ForeignKey(
                        name: "FK_FleetWashLogs_BookingDetails_BookingDetailId",
                        column: x => x.BookingDetailId,
                        principalTable: "BookingDetails",
                        principalColumn: "DetailId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FleetWashLogs_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FleetWashLogs_FleetVehicles_FleetVehicleId",
                        column: x => x.FleetVehicleId,
                        principalTable: "FleetVehicles",
                        principalColumn: "FleetVehicleId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FleetVehicleId",
                table: "Bookings",
                column: "FleetVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetWashLogs_BookingDetailId",
                table: "FleetWashLogs",
                column: "BookingDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetWashLogs_BranchId",
                table: "FleetWashLogs",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetWashLogs_FleetVehicleId",
                table: "FleetWashLogs",
                column: "FleetVehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_FleetVehicles_FleetVehicleId",
                table: "Bookings",
                column: "FleetVehicleId",
                principalTable: "FleetVehicles",
                principalColumn: "FleetVehicleId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_FleetVehicles_FleetVehicleId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "FleetWashLogs");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_FleetVehicleId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "FleetVehicleId",
                table: "Bookings");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualPrice",
                table: "BookingDetails",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualVehicleTypeId",
                table: "BookingDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttendanceStatus",
                table: "BookingDetails",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CapacityWeight",
                table: "BookingDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInTime",
                table: "BookingDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutTime",
                table: "BookingDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "BookingDetails",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DepositStatus",
                table: "BookingDetails",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "BookingDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MismatchSurcharge",
                table: "BookingDetails",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingLaneId",
                table: "BookingDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingStaffId",
                table: "BookingDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BookingDetails",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "VehicleCondition",
                table: "BookingDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "BookingDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingDetails_ActualVehicleTypeId",
                table: "BookingDetails",
                column: "ActualVehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDetails_InvoiceId",
                table: "BookingDetails",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDetails_ProcessingLaneId",
                table: "BookingDetails",
                column: "ProcessingLaneId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDetails_ProcessingStaffId",
                table: "BookingDetails",
                column: "ProcessingStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDetails_VehicleId",
                table: "BookingDetails",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetails_Invoices_InvoiceId",
                table: "BookingDetails",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetails_Lanes_ProcessingLaneId",
                table: "BookingDetails",
                column: "ProcessingLaneId",
                principalTable: "Lanes",
                principalColumn: "LaneId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetails_Users_ProcessingStaffId",
                table: "BookingDetails",
                column: "ProcessingStaffId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetails_VehicleTypes_ActualVehicleTypeId",
                table: "BookingDetails",
                column: "ActualVehicleTypeId",
                principalTable: "VehicleTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetails_Vehicles_VehicleId",
                table: "BookingDetails",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id");
        }
    }
}
