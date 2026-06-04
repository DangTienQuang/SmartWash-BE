using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetails_Vehicles_VehicleLicensePlate",
                table: "BookingDetails");

            migrationBuilder.AddColumn<bool>(
                name: "IsBusinessLane",
                table: "Lanes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractEndDate",
                table: "BusinessProfiles",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractStartDate",
                table: "BusinessProfiles",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentMonthUsage",
                table: "BusinessProfiles",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "BusinessProfiles",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsContractActive",
                table: "BusinessProfiles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyCreditLimit",
                table: "BusinessProfiles",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "FleetImportBatches",
                columns: table => new
                {
                    FleetImportBatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BusinessProfileId = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    SuccessRows = table.Column<int>(type: "int", nullable: false),
                    FailedRows = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetImportBatches", x => x.FleetImportBatchId);
                    table.ForeignKey(
                        name: "FK_FleetImportBatches_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "BusinessProfileId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FleetVehicles",
                columns: table => new
                {
                    FleetVehicleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BusinessProfileId = table.Column<int>(type: "int", nullable: false),
                    LicensePlate = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VehicleTypeId = table.Column<int>(type: "int", nullable: false),
                    Brand = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DriverName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetVehicles", x => x.FleetVehicleId);
                    table.ForeignKey(
                        name: "FK_FleetVehicles_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "BusinessProfileId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FleetVehicles_VehicleTypes_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "VehicleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FleetImportErrors",
                columns: table => new
                {
                    FleetImportErrorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FleetImportBatchId = table.Column<int>(type: "int", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetImportErrors", x => x.FleetImportErrorId);
                    table.ForeignKey(
                        name: "FK_FleetImportErrors_FleetImportBatches_FleetImportBatchId",
                        column: x => x.FleetImportBatchId,
                        principalTable: "FleetImportBatches",
                        principalColumn: "FleetImportBatchId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProfiles_ReviewedByUserId",
                table: "BusinessProfiles",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetImportBatches_BusinessProfileId",
                table: "FleetImportBatches",
                column: "BusinessProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetImportErrors_FleetImportBatchId",
                table: "FleetImportErrors",
                column: "FleetImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetVehicles_BusinessProfileId",
                table: "FleetVehicles",
                column: "BusinessProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetVehicles_LicensePlate",
                table: "FleetVehicles",
                column: "LicensePlate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FleetVehicles_VehicleTypeId",
                table: "FleetVehicles",
                column: "VehicleTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetails_Vehicles_VehicleLicensePlate",
                table: "BookingDetails",
                column: "VehicleLicensePlate",
                principalTable: "Vehicles",
                principalColumn: "LicensePlate");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessProfiles_Users_ReviewedByUserId",
                table: "BusinessProfiles",
                column: "ReviewedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetails_Vehicles_VehicleLicensePlate",
                table: "BookingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessProfiles_Users_ReviewedByUserId",
                table: "BusinessProfiles");

            migrationBuilder.DropTable(
                name: "FleetImportErrors");

            migrationBuilder.DropTable(
                name: "FleetVehicles");

            migrationBuilder.DropTable(
                name: "FleetImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_BusinessProfiles_ReviewedByUserId",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "IsBusinessLane",
                table: "Lanes");

            migrationBuilder.DropColumn(
                name: "ContractEndDate",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "ContractStartDate",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "CurrentMonthUsage",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "IsContractActive",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "MonthlyCreditLimit",
                table: "BusinessProfiles");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetails_Vehicles_VehicleLicensePlate",
                table: "BookingDetails",
                column: "VehicleLicensePlate",
                principalTable: "Vehicles",
                principalColumn: "LicensePlate",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
