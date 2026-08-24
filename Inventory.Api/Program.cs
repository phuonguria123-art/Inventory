using AutoMapper;
using Inventory.Application.Interfaces;
using Inventory.Application.Mappings;
using Inventory.Application.Products.Services;
using Inventory.Application.Users.Services;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Authentication;
using Inventory.Infrastructure.Context;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Inventory.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["AppSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["AppSettings:Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]!)),
            ValidateIssuerSigningKey = true
        };
    });

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DBConnection")));

            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
            builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();

            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            //map exception sang JSON
            app.UseExceptionHandler(exceptionHandler =>
            {
                exceptionHandler.Run(async context =>
                {
                    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                    var statusCode = exception switch
                    {
                        NotFoundException => StatusCodes.Status404NotFound,
                        ValidationException => StatusCodes.Status400BadRequest,
                        _ => StatusCodes.Status500InternalServerError
                    };

                    context.Response.StatusCode = statusCode;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = exception?.Message ?? "Có lỗi xảy ra",
                        errors = new[] { exception?.Message ?? "Có lỗi xảy ra" }
                    });
                });
            });

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
