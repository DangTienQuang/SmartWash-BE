using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherCampaignRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CampaignType",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Vouchers",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CurrentUsageCount",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExpiryDays",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InactiveDays",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Vouchers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsagePerUser",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MilestoneUsageCount",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinOrderAmount",
                table: "Vouchers",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ResendAfterDays",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Vouchers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetAge",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedDate",
                table: "UserVouchers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedDate",
                table: "UserVouchers",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TriggerKey",
                table: "UserVouchers",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "UsageCount",
                table: "UserVouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Code",
                table: "Vouchers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_UserId_VoucherId_TriggerKey",
                table: "UserVouchers",
                columns: new[] { "UserId", "VoucherId", "TriggerKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Code",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_UserVouchers_UserId_VoucherId_TriggerKey",
                table: "UserVouchers");

            migrationBuilder.DropColumn(
                name: "CampaignType",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "CurrentUsageCount",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ExpiryDays",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "InactiveDays",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "MaxUsagePerUser",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "MilestoneUsageCount",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "MinOrderAmount",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ResendAfterDays",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "TargetAge",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "LastUsedDate",
                table: "UserVouchers");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "UserVouchers");

            migrationBuilder.DropColumn(
                name: "TriggerKey",
                table: "UserVouchers");

            migrationBuilder.DropColumn(
                name: "UsageCount",
                table: "UserVouchers");
        }
    }
}
