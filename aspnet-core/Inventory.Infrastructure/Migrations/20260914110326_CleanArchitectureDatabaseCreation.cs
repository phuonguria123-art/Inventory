using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanArchitectureDatabaseCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Products_ProductId",
                table: "InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Warehouses_WarehouseId",
                table: "InventoryItems");

            // Add the new foreign key as nullable first so existing lot rows can
            // be mapped from their old WarehouseId + ProductId values.
            migrationBuilder.AddColumn<Guid>(
                name: "InventoryId",
                table: "InventoryItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE inventoryItem
                SET inventoryItem.InventoryId = inventory.Id
                FROM InventoryItems AS inventoryItem
                INNER JOIN Inventories AS inventory
                    ON inventory.WarehouseId = inventoryItem.WarehouseId
                    AND inventory.ProductId = inventoryItem.ProductId;
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM InventoryItems WHERE InventoryId IS NULL)
                BEGIN
                    THROW 50001, 'InventoryItem does not have a matching inventory balance.', 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT InventoryId, BatchNumber
                    FROM InventoryItems
                    GROUP BY InventoryId, BatchNumber
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 50002, 'Duplicate batch numbers exist in the same inventory balance.', 1;
                END;
                """);

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_ProductId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_WarehouseId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "InventoryItems");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "InventoryItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "BatchNumber",
                table: "InventoryItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<Guid>(
                name: "InventoryId",
                table: "InventoryItems",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_InventoryId_BatchNumber",
                table: "InventoryItems",
                columns: new[] { "InventoryId", "BatchNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryItems_ExpiryDate",
                table: "InventoryItems",
                sql: "[ExpiryDate] IS NULL OR [ManufactureDate] IS NULL OR [ExpiryDate] >= [ManufactureDate]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryItems_Quantity",
                table: "InventoryItems",
                sql: "[Quantity] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryItems_UnitCost",
                table: "InventoryItems",
                sql: "[UnitCost] >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Inventories_InventoryId",
                table: "InventoryItems",
                column: "InventoryId",
                principalTable: "Inventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Inventories_InventoryId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_InventoryId_BatchNumber",
                table: "InventoryItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryItems_ExpiryDate",
                table: "InventoryItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryItems_Quantity",
                table: "InventoryItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryItems_UnitCost",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "InventoryId",
                table: "InventoryItems");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "BatchNumber",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "InventoryItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "InventoryItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Products_ProductId",
                table: "InventoryItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Warehouses_WarehouseId",
                table: "InventoryItems",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
