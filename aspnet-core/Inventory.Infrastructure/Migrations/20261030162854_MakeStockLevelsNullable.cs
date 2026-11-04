using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeStockLevelsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MinStock",
                table: "Inventories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "MaxStock",
                table: "Inventories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Existing balances were created before stock levels were configurable,
            // so 0/0 means "not configured" rather than a real threshold pair.
            migrationBuilder.Sql(
                "UPDATE [Inventories] SET [MinStock] = NULL, [MaxStock] = NULL " +
                "WHERE [MinStock] = 0 AND [MaxStock] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MinStock",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxStock",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
