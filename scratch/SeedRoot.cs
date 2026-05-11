using HierarchyAccounts.Infrastructure.Data;
using HierarchyAccounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("HierarchyAccounts.Api/appsettings.json")
    .Build();

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

using var context = new AppDbContext(optionsBuilder.Options);

Console.WriteLine("Checking existing accounts...");

Console.WriteLine("Clearing existing accounts to apply new seed...");
context.Accounts.RemoveRange(context.Accounts);
await context.SaveChangesAsync();

Console.WriteLine("Database cleared. Starting fresh seed...");
    
    // Level 1: Root
    var root = Account.CreateRoot("Global Corp");
    await context.Accounts.AddAsync(root);
    await context.SaveChangesAsync();

    // Level 2: Regions
    var europe = Account.CreateChild("Europe Region", root);
    var americas = Account.CreateChild("Americas Region", root);
    var asia = Account.CreateChild("Asia Pacific", root);
    await context.Accounts.AddRangeAsync(europe, americas, asia);
    await context.SaveChangesAsync();

    // Level 3: Countries
    var bg = Account.CreateChild("Bulgaria", europe);
    var de = Account.CreateChild("Germany", europe);
    var usa = Account.CreateChild("United States", americas);
    var jp = Account.CreateChild("Japan", asia);
    await context.Accounts.AddRangeAsync(bg, de, usa, jp);
    await context.SaveChangesAsync();

    // Level 4: Cities / Offices
    var sofia = Account.CreateChild("Sofia HQ", bg);
    var plovdiv = Account.CreateChild("Plovdiv Branch", bg);
    var berlin = Account.CreateChild("Berlin Office", de);
    var ny = Account.CreateChild("New York Branch", usa);
    var sf = Account.CreateChild("San Francisco Lab", usa);
    var tokyo = Account.CreateChild("Tokyo Central", jp);
    await context.Accounts.AddRangeAsync(sofia, plovdiv, berlin, ny, sf, tokyo);
    await context.SaveChangesAsync();

    // Level 5: Departments / Teams (MAX DEPTH reached)
    var devTeam = Account.CreateChild("Software Development", sofia);
    var qaTeam = Account.CreateChild("Quality Assurance", sofia);
    var salesTeam = Account.CreateChild("Enterprise Sales", ny);
    var aiLab = Account.CreateChild("AI Research Group", sf);
    await context.Accounts.AddRangeAsync(devTeam, qaTeam, salesTeam, aiLab);
    await context.SaveChangesAsync();


Console.WriteLine("Diverse hierarchy seeded successfully!");

