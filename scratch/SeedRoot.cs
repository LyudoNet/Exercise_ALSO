using HierarchyAccounts.Infrastructure.Data;
using HierarchyAccounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("HierarchyAccounts.Api/appsettings.json")
    .Build();

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

using var context = new AppDbContext(optionsBuilder.Options);

if (!await context.Accounts.AnyAsync(a => a.ParentId == null))
{
    Console.WriteLine("Creating root account 'Global Corp'...");
    var root = Account.CreateRoot("Global Corp");
    await context.Accounts.AddAsync(root);
    await context.SaveChangesAsync();
    Console.WriteLine($"Root account created with ID: {root.Id}");
}
else
{
    Console.WriteLine("Root account already exists.");
}
