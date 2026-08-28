using AutoMapper;
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
            CreateMap<Supplier, UpdateWarehouseDto>().ReverseMap();

            CreateMap<User, UserProfileDto>()
    .ForMember(
        destination => destination.Roles,
        options => options.MapFrom(source =>
            source.UserRoles.Select(userRole =>
                userRole.Role.Name)));

            //  CreateMap<Product, ProductDto>()
            //.ForMember(
            //    dest => dest.ProductName,
            //    opt => opt.MapFrom(src => src.Name)
            //);

            //  CreateMap<ProductDto, Product>();
        }
    }
}
