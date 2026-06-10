using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VehicleTypeId",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_VehicleTypeId",
                table: "Vouchers",
                column: "VehicleTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_VehicleTypes_VehicleTypeId",
                table: "Vouchers",
                column: "VehicleTypeId",
                principalTable: "VehicleTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_VehicleTypes_VehicleTypeId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_VehicleTypeId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "VehicleTypeId",
                table: "Vouchers");
        }
    }
}
