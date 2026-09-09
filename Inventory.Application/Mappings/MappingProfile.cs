using AutoMapper;
using Inventory.Application.Inventory.Dto;
using Inventory.Application.Products.DTOs;
using Inventory.Application.PurchaseOrders.Dto;
using Inventory.Application.Suppliers.Dto;
using Inventory.Application.Users.DTOs;
using Inventory.Application.Warehouses.DTOs;
using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Product, ProductDto>();
            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();

            CreateMap<UpdatePurchaseOrderDto, PurchanseOrder>();

            CreateMap<Warehouse, WarehouseDto>().ReverseMap();
            CreateMap<Warehouse, CreateWarehouseDto>().ReverseMap();
            CreateMap<Warehouse, UpdateWarehouseDto>().ReverseMap();

            CreateMap<Supplier, SupplierDto>().ReverseMap();
            CreateMap<Supplier, CreateSupplierDto>().ReverseMap();

            CreateMap<Supplier, UpdateSupplierDto>().ReverseMap();

            CreateMap<User, UserProfileDto>()
                .ForMember(
                    destination => destination.Roles,
                    options => options.MapFrom(source =>
                        source.UserRoles.Select(userRole => userRole.Role)));
            CreateMap<Permission, PermissionDto>();
            CreateMap<Role, RoleDto>()
                .ForMember(
                    destination => destination.Permissions,
                    options => options.MapFrom(source =>
                        source.RolePermissions.Select(rolePermission => rolePermission.Permission)));

            CreateMap<Inventories, InventoryDto>()
                .ForMember(destination => destination.WarehouseCode,
                    options => options.MapFrom(source => source.Warehouse.Code))
                .ForMember(destination => destination.WarehouseName,
                    options => options.MapFrom(source => source.Warehouse.Name))
                .ForMember(destination => destination.ProductCode,
                    options => options.MapFrom(source => source.Product.Code))
                .ForMember(destination => destination.ProductName,
                    options => options.MapFrom(source => source.Product.Name));
            CreateMap<InventoryTransaction, InventoryTransactionDto>();
            //  CreateMap<Product, ProductDto>()
            //.ForMember(
            //    dest => dest.ProductName,
            //    opt => opt.MapFrom(src => src.Name)
            //);

            //  CreateMap<ProductDto, Product>();
        }
    }
}
