using Inventory.Application.Authorization;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Context;

internal static class AuthorizationSeedData
{
    private static readonly Guid AdminRoleId = new("10000000-0000-0000-0000-000000000001");
    private static readonly Guid ManagerRoleId = new("10000000-0000-0000-0000-000000000002");
    private static readonly Guid StaffRoleId = new("10000000-0000-0000-0000-000000000003");

    internal static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = AdminRoleId,
                Name = nameof(RoleName.Admin),
                Description = "Quản trị toàn bộ hệ thống"
            },
            new Role
            {
                Id = ManagerRoleId,
                Name = nameof(RoleName.Manager),
                Description = "Quản lý hoạt động kho và dữ liệu nghiệp vụ"
            },
            new Role
            {
                Id = StaffRoleId,
                Name = nameof(RoleName.Staff),
                Description = "Nhân viên thực hiện các nghiệp vụ kho"
            });

        var permissions = CreatePermissions();
        modelBuilder.Entity<Permission>().HasData(permissions);

        var managerPermissionCodes = new HashSet<string>
        {
            PermissionCodes.ProductRead,
            PermissionCodes.ProductCreate,
            PermissionCodes.ProductUpdate,
            PermissionCodes.WarehouseRead,
            PermissionCodes.WarehouseCreate,
            PermissionCodes.WarehouseUpdate,
            PermissionCodes.SupplierRead,
            PermissionCodes.SupplierCreate,
            PermissionCodes.SupplierUpdate,
            PermissionCodes.InventoryRead,
            PermissionCodes.InventoryReceive,
            PermissionCodes.InventoryIssue,
            PermissionCodes.InventoryTransfer,
            PermissionCodes.InventoryAdjust,
            PermissionCodes.AuditRead
        };

        var staffPermissionCodes = new HashSet<string>
        {
            PermissionCodes.ProductRead,
            PermissionCodes.WarehouseRead,
            PermissionCodes.SupplierRead,
            PermissionCodes.InventoryRead,
            PermissionCodes.InventoryReceive,
            PermissionCodes.InventoryIssue,
            PermissionCodes.InventoryTransfer
        };

        var rolePermissions = permissions
            .Select(permission => new RolePermission
            {
                RoleId = AdminRoleId,
                PermissionId = permission.Id
            })
            .Concat(permissions
                .Where(permission => managerPermissionCodes.Contains(permission.Code))
                .Select(permission => new RolePermission
                {
                    RoleId = ManagerRoleId,
                    PermissionId = permission.Id
                }))
            .Concat(permissions
                .Where(permission => staffPermissionCodes.Contains(permission.Code))
                .Select(permission => new RolePermission
                {
                    RoleId = StaffRoleId,
                    PermissionId = permission.Id
                }))
            .ToArray();

        modelBuilder.Entity<RolePermission>().HasData(rolePermissions);
    }

    private static Permission[] CreatePermissions()
    {
        return
        [
            CreatePermission(1, PermissionCodes.ProductRead, "Xem sản phẩm"),
            CreatePermission(2, PermissionCodes.ProductCreate, "Tạo sản phẩm"),
            CreatePermission(3, PermissionCodes.ProductUpdate, "Cập nhật sản phẩm"),
            CreatePermission(4, PermissionCodes.ProductDelete, "Xóa sản phẩm"),

            CreatePermission(5, PermissionCodes.WarehouseRead, "Xem kho hàng"),
            CreatePermission(6, PermissionCodes.WarehouseCreate, "Tạo kho hàng"),
            CreatePermission(7, PermissionCodes.WarehouseUpdate, "Cập nhật kho hàng"),
            CreatePermission(8, PermissionCodes.WarehouseDelete, "Xóa kho hàng"),

            CreatePermission(9, PermissionCodes.SupplierRead, "Xem nhà cung cấp"),
            CreatePermission(10, PermissionCodes.SupplierCreate, "Tạo nhà cung cấp"),
            CreatePermission(11, PermissionCodes.SupplierUpdate, "Cập nhật nhà cung cấp"),
            CreatePermission(12, PermissionCodes.SupplierDelete, "Xóa nhà cung cấp"),

            CreatePermission(13, PermissionCodes.InventoryRead, "Xem tồn kho"),
            CreatePermission(14, PermissionCodes.InventoryReceive, "Nhập hàng vào kho"),
            CreatePermission(15, PermissionCodes.InventoryIssue, "Xuất hàng khỏi kho"),
            CreatePermission(16, PermissionCodes.InventoryTransfer, "Chuyển hàng giữa các kho"),
            CreatePermission(17, PermissionCodes.InventoryAdjust, "Điều chỉnh tồn kho"),

            CreatePermission(18, PermissionCodes.UserRead, "Xem người dùng"),
            CreatePermission(19, PermissionCodes.UserManage, "Quản lý người dùng"),
            CreatePermission(20, PermissionCodes.RoleRead, "Xem vai trò"),
            CreatePermission(21, PermissionCodes.RoleManage, "Quản lý vai trò và phân quyền"),
            CreatePermission(22, PermissionCodes.PermissionRead, "Xem danh mục quyền"),
            CreatePermission(23, PermissionCodes.AuditRead, "Xem lịch sử kiểm toán")
        ];
    }

    private static Permission CreatePermission(int sequence, string code, string description)
    {
        return new Permission
        {
            Id = new Guid($"20000000-0000-0000-0000-{sequence:D12}"),
            Code = code,
            Description = description
        };
    }
}
