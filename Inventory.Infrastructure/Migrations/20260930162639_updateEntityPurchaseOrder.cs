using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateEntityPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchanseOrdersDetails_Products_ProductId",
                table: "PurchanseOrdersDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchanseOrdersDetails_PurchanseOrders_PurchanseOrderId",
                table: "PurchanseOrdersDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchanseOrdersDetails",
                table: "PurchanseOrdersDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchanseOrders",
                table: "PurchanseOrders");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000002") });

            migrationBuilder.RenameTable(
                name: "PurchanseOrdersDetails",
                newName: "PurchaseOrdersDetails");

            migrationBuilder.RenameTable(
                name: "PurchanseOrders",
                newName: "PurchaseOrders");

            migrationBuilder.RenameIndex(
                name: "IX_PurchanseOrdersDetails_PurchanseOrderId",
                table: "PurchaseOrdersDetails",
                newName: "IX_PurchaseOrdersDetails_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchanseOrdersDetails_ProductId",
                table: "PurchaseOrdersDetails",
                newName: "IX_PurchaseOrdersDetails_ProductId");
            migrationBuilder.RenameColumn(
                name: "PurchanseOrderId",
                table: "PurchaseOrdersDetails",
                newName: "PurchaseOrderId");

            migrationBuilder.RenameColumn(
                name: "ReiceiveDate",
                table: "PurchaseOrders",
                newName: "ReceivedDate");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "PurchaseOrdersDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReceiveTime",
                table: "PurchaseOrdersDetails",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "InventoryId",
                table: "PurchaseOrdersDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "PurchaseOrders",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseOrdersDetails",
                table: "PurchaseOrdersDetails",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseOrders",
                table: "PurchaseOrders",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000018"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "order.read", "Xem đơn mua hàng" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000019"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "order.create", "Tạo đơn mua hàng" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000020"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "order.update", "Cập nhật đơn mua hàng" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000021"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "order.receive", "Nhận hàng đơn mua hàng" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000022"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "order.cancel", "Hủy đơn mua hàng" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000023"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "user.read", "Xem người dùng" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000024"), "user.manage", "Quản lý người dùng" },
                    { new Guid("20000000-0000-0000-0000-000000000025"), "role.read", "Xem vai trò" },
                    { new Guid("20000000-0000-0000-0000-000000000026"), "role.manage", "Quản lý vai trò và phân quyền" },
                    { new Guid("20000000-0000-0000-0000-000000000027"), "permission.read", "Xem danh mục quyền" },
                    { new Guid("20000000-0000-0000-0000-000000000028"), "audit.read", "Xem lịch sử kiểm toán" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000027"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000028"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000028"), new Guid("10000000-0000-0000-0000-000000000002") }
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PurchaseOrderDetail_ActualReceivedQuantity",
                table: "PurchaseOrdersDetails",
                sql: "[ActualReceivedQuantity] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PurchaseOrderDetail_OrderedQuantity",
                table: "PurchaseOrdersDetails",
                sql: "[OrderedQuantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PurchaseOrderDetail_UnitPrice",
                table: "PurchaseOrdersDetails",
                sql: "[UnitPrice] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Code",
                table: "PurchaseOrders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ReceivingWarehouseId",
                table: "PurchaseOrders",
                column: "ReceivingWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Warehouses_ReceivingWarehouseId",
                table: "PurchaseOrders",
                column: "ReceivingWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrdersDetails_Products_ProductId",
                table: "PurchaseOrdersDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrdersDetails_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrdersDetails",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Warehouses_ReceivingWarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrdersDetails_Products_ProductId",
                table: "PurchaseOrdersDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrdersDetails_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrdersDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseOrdersDetails",
                table: "PurchaseOrdersDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PurchaseOrderDetail_ActualReceivedQuantity",
                table: "PurchaseOrdersDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PurchaseOrderDetail_OrderedQuantity",
                table: "PurchaseOrdersDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PurchaseOrderDetail_UnitPrice",
                table: "PurchaseOrdersDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseOrders",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_Code",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_ReceivingWarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000027"), new Guid("10000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000028"), new Guid("10000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000028"), new Guid("10000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000027"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000028"));

            migrationBuilder.RenameTable(
                name: "PurchaseOrdersDetails",
                newName: "PurchanseOrdersDetails");

            migrationBuilder.RenameTable(
                name: "PurchaseOrders",
                newName: "PurchanseOrders");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrdersDetails_PurchaseOrderId",
                table: "PurchanseOrdersDetails",
                newName: "IX_PurchanseOrdersDetails_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrdersDetails_ProductId",
                table: "PurchanseOrdersDetails",
                newName: "IX_PurchanseOrdersDetails_ProductId");
            migrationBuilder.RenameColumn(
                name: "PurchaseOrderId",
                table: "PurchanseOrdersDetails",
                newName: "PurchanseOrderId");
            migrationBuilder.RenameColumn(
                name: "ReceivedDate",
                table: "PurchanseOrders",
                newName: "ReiceiveDate");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "PurchanseOrdersDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReceiveTime",
                table: "PurchanseOrdersDetails",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InventoryId",
                table: "PurchanseOrdersDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "PurchanseOrders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchanseOrdersDetails",
                table: "PurchanseOrdersDetails",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchanseOrders",
                table: "PurchanseOrders",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000018"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "user.read", "Xem người dùng" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000019"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "user.manage", "Quản lý người dùng" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000020"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "role.read", "Xem vai trò" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000021"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "role.manage", "Quản lý vai trò và phân quyền" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000022"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "permission.read", "Xem danh mục quyền" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000023"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "audit.read", "Xem lịch sử kiểm toán" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[] { new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000002") });

            migrationBuilder.AddForeignKey(
                name: "FK_PurchanseOrdersDetails_Products_ProductId",
                table: "PurchanseOrdersDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchanseOrdersDetails_PurchanseOrders_PurchanseOrderId",
                table: "PurchanseOrdersDetails",
                column: "PurchanseOrderId",
                principalTable: "PurchanseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
