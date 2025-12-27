using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCar.Migrations
{
    /// <inheritdoc />
    public partial class AddHandoverModuleColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DamagesFound",
                table: "ReturnRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorCondition",
                table: "ReturnRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FuelShortage",
                table: "ReturnRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InteriorCondition",
                table: "ReturnRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsCleaning",
                table: "ReturnRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsMaintenance",
                table: "ReturnRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceivedBy",
                table: "ReturnRecords",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "CustomerConfirmed",
                table: "HandoverRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerConfirmedAt",
                table: "HandoverRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorCondition",
                table: "HandoverRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HandedOverBy",
                table: "HandoverRecords",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "InteriorCondition",
                table: "HandoverRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreExistingDamages",
                table: "HandoverRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HandoverAccessories",
                columns: table => new
                {
                    AccessoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccessoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    IsReturnedOk = table.Column<bool>(type: "bit", nullable: false),
                    DamageNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstimatedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverAccessories", x => x.AccessoryId);
                    table.ForeignKey(
                        name: "FK_HandoverAccessories_HandoverRecords_HandoverId",
                        column: x => x.HandoverId,
                        principalTable: "HandoverRecords",
                        principalColumn: "HandoverId");
                    table.ForeignKey(
                        name: "FK_HandoverAccessories_ReturnRecords_ReturnId",
                        column: x => x.ReturnId,
                        principalTable: "ReturnRecords",
                        principalColumn: "ReturnId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRecords_ReceivedBy",
                table: "ReturnRecords",
                column: "ReceivedBy");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverRecords_HandedOverBy",
                table: "HandoverRecords",
                column: "HandedOverBy");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverAccessories_HandoverId",
                table: "HandoverAccessories",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverAccessories_ReturnId",
                table: "HandoverAccessories",
                column: "ReturnId");

            migrationBuilder.AddForeignKey(
                name: "FK_HandoverRecords_UserAccounts_HandedOverBy",
                table: "HandoverRecords",
                column: "HandedOverBy",
                principalTable: "UserAccounts",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRecords_UserAccounts_ReceivedBy",
                table: "ReturnRecords",
                column: "ReceivedBy",
                principalTable: "UserAccounts",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HandoverRecords_UserAccounts_HandedOverBy",
                table: "HandoverRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRecords_UserAccounts_ReceivedBy",
                table: "ReturnRecords");

            migrationBuilder.DropTable(
                name: "HandoverAccessories");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRecords_ReceivedBy",
                table: "ReturnRecords");

            migrationBuilder.DropIndex(
                name: "IX_HandoverRecords_HandedOverBy",
                table: "HandoverRecords");

            migrationBuilder.DropColumn(
                name: "DamagesFound",
                table: "ReturnRecords");

            migrationBuilder.DropColumn(
                name: "ExteriorCondition",
                table: "ReturnRecords");

            migrationBuilder.DropColumn(
                name: "FuelShortage",
                table: "ReturnRecords");

            migrationBuilder.DropColumn(
                name: "InteriorCondition",
                table: "ReturnRecords");

            migrationBuilder.DropColumn(
                name: "NeedsCleaning",
                table: "ReturnRecords");

            migrationBuilder.DropColumn(
                name: "NeedsMaintenance",
                table: "ReturnRecords");

            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                table: "ReturnRecords");

            migrationBuilder.DropColumn(
                name: "CustomerConfirmed",
                table: "HandoverRecords");

            migrationBuilder.DropColumn(
                name: "CustomerConfirmedAt",
                table: "HandoverRecords");

            migrationBuilder.DropColumn(
                name: "ExteriorCondition",
                table: "HandoverRecords");

            migrationBuilder.DropColumn(
                name: "HandedOverBy",
                table: "HandoverRecords");

            migrationBuilder.DropColumn(
                name: "InteriorCondition",
                table: "HandoverRecords");

            migrationBuilder.DropColumn(
                name: "PreExistingDamages",
                table: "HandoverRecords");
        }
    }
}
