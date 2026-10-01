using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservations", x => x.Id);
                    table.CheckConstraint("CK_InventoryReservations_ExpiresAt", "[ExpiresAt] IS NULL OR [ExpiresAt] > [CreatedAt]");
                    table.CheckConstraint("CK_InventoryReservations_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_InventoryReservations_Reference", "LEN(LTRIM(RTRIM([Reference]))) > 0");
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Inventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_CreatedByUserId",
                table: "InventoryReservations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ExpiresAt",
                table: "InventoryReservations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_InventoryId_Reference",
                table: "InventoryReservations",
                columns: new[] { "InventoryId", "Reference" },
                unique: true,
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_InventoryId_Status",
                table: "InventoryReservations",
                columns: new[] { "InventoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_Reference",
                table: "InventoryReservations",
                column: "Reference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryReservations");
        }
    }
}
