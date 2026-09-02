namespace Inventory.Application.Authorization;

public static class PermissionCodes
{
    public const string ProductRead = "product.read";
    public const string ProductCreate = "product.create";
    public const string ProductUpdate = "product.update";
    public const string ProductDelete = "product.delete";

    public const string WarehouseRead = "warehouse.read";
    public const string WarehouseCreate = "warehouse.create";
    public const string WarehouseUpdate = "warehouse.update";
    public const string WarehouseDelete = "warehouse.delete";

    public const string SupplierRead = "supplier.read";
    public const string SupplierCreate = "supplier.create";
    public const string SupplierUpdate = "supplier.update";
    public const string SupplierDelete = "supplier.delete";

    public const string InventoryRead = "inventory.read";
    public const string InventoryReceive = "inventory.receive";
    public const string InventoryIssue = "inventory.issue";
    public const string InventoryTransfer = "inventory.transfer";
    public const string InventoryAdjust = "inventory.adjust";

    public const string UserRead = "user.read";
    public const string UserManage = "user.manage";
    public const string RoleRead = "role.read";
    public const string RoleManage = "role.manage";
    public const string PermissionRead = "permission.read";
    public const string AuditRead = "audit.read";

    public static readonly IReadOnlyCollection<string> All =
   [
        ProductRead,
    ProductCreate,
    ProductUpdate,
    ProductDelete,

    WarehouseRead,
    WarehouseCreate,
    WarehouseUpdate,
    WarehouseDelete,

    SupplierRead,
    SupplierCreate,
    SupplierUpdate,
    SupplierDelete,

    InventoryRead,
    InventoryReceive,
    InventoryIssue,
    InventoryTransfer,
    InventoryAdjust,

    UserRead,
    UserManage,
    RoleRead,
    RoleManage,
    PermissionRead,
    AuditRead
    ];
}
