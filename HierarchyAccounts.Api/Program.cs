using HierarchyAccounts.Application.Interfaces;
using HierarchyAccounts.Application.Services;
using HierarchyAccounts.Domain.Interfaces;
using HierarchyAccounts.Infrastructure.Data;
using HierarchyAccounts.Infrastructure.Repositories;
using HierarchyAccounts.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Register SQL Server DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repository and service with scoped lifetime (one instance per HTTP request)
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddControllers();

// Configure Swagger/OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GUHC Hierarchy Accounts API",
        Version = "v1",
        Description = "Manages account hierarchies for Grand Unified Holding Corp. (GUHC). " +
                      "Supports up to 5 levels of depth. Cycles are prevented automatically."
    });
});

var app = builder.Build();

// Register global exception handling middleware — must be first in the pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GUHC Hierarchy Accounts API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
