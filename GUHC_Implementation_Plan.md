# GUHC Hierarchy Accounts System — Full Implementation Plan

> **Instructions for the AI Agent:**
> Follow this plan step by step, in order. Do not skip phases. All code comments must be in English.
> Create every file exactly as specified. Commit after each phase as instructed.

---

## PHASE 1: SOLUTION & PROJECT STRUCTURE

### Step 1.1 — Create Solution and Projects

Run the following commands from an empty root folder:

```bash
# Create solution
dotnet new sln -n HierarchyAccounts

# Create projects
dotnet new webapi -n HierarchyAccounts.Api --framework net8.0
dotnet new classlib -n HierarchyAccounts.Domain --framework net8.0
dotnet new classlib -n HierarchyAccounts.Application --framework net8.0
dotnet new classlib -n HierarchyAccounts.Infrastructure --framework net8.0
dotnet new console -n HierarchyAccounts.Console --framework net8.0
dotnet new xunit -n HierarchyAccounts.Tests --framework net8.0

# Add all projects to the solution
dotnet sln add HierarchyAccounts.Api/HierarchyAccounts.Api.csproj
dotnet sln add HierarchyAccounts.Domain/HierarchyAccounts.Domain.csproj
dotnet sln add HierarchyAccounts.Application/HierarchyAccounts.Application.csproj
dotnet sln add HierarchyAccounts.Infrastructure/HierarchyAccounts.Infrastructure.csproj
dotnet sln add HierarchyAccounts.Console/HierarchyAccounts.Console.csproj
dotnet sln add HierarchyAccounts.Tests/HierarchyAccounts.Tests.csproj
```

### Step 1.2 — Add Project References

Dependency direction: `Api → Application → Domain`, `Api → Infrastructure → Domain`, `Tests → all`

```bash
dotnet add HierarchyAccounts.Api/HierarchyAccounts.Api.csproj reference \
  HierarchyAccounts.Application/HierarchyAccounts.Application.csproj

dotnet add HierarchyAccounts.Api/HierarchyAccounts.Api.csproj reference \
  HierarchyAccounts.Infrastructure/HierarchyAccounts.Infrastructure.csproj

dotnet add HierarchyAccounts.Application/HierarchyAccounts.Application.csproj reference \
  HierarchyAccounts.Domain/HierarchyAccounts.Domain.csproj

dotnet add HierarchyAccounts.Infrastructure/HierarchyAccounts.Infrastructure.csproj reference \
  HierarchyAccounts.Application/HierarchyAccounts.Application.csproj

dotnet add HierarchyAccounts.Infrastructure/HierarchyAccounts.Infrastructure.csproj reference \
  HierarchyAccounts.Domain/HierarchyAccounts.Domain.csproj

dotnet add HierarchyAccounts.Tests/HierarchyAccounts.Tests.csproj reference \
  HierarchyAccounts.Domain/HierarchyAccounts.Domain.csproj

dotnet add HierarchyAccounts.Tests/HierarchyAccounts.Tests.csproj reference \
  HierarchyAccounts.Application/HierarchyAccounts.Application.csproj

dotnet add HierarchyAccounts.Tests/HierarchyAccounts.Tests.csproj reference \
  HierarchyAccounts.Infrastructure/HierarchyAccounts.Infrastructure.csproj
```

### Step 1.3 — Install NuGet Packages

```bash
# Infrastructure
dotnet add HierarchyAccounts.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add HierarchyAccounts.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add HierarchyAccounts.Infrastructure package Microsoft.EntityFrameworkCore.Tools

# Api
dotnet add HierarchyAccounts.Api package Microsoft.EntityFrameworkCore.Design
dotnet add HierarchyAccounts.Api package Swashbuckle.AspNetCore

# Tests
dotnet add HierarchyAccounts.Tests package Microsoft.EntityFrameworkCore.InMemory
dotnet add HierarchyAccounts.Tests package Moq
dotnet add HierarchyAccounts.Tests package FluentAssertions
```

### Step 1.4 — Required Folder Structure

Create all folders and placeholder files so the structure below exists before writing any code.

```
HierarchyAccounts/
├── HierarchyAccounts.sln
│
├── HierarchyAccounts.Domain/
│   ├── Entities/
│   │   └── Account.cs
│   ├── Exceptions/
│   │   ├── DomainException.cs
│   │   ├── CycleDetectedException.cs
│   │   ├── MaxDepthExceededException.cs
│   │   └── RootAccountException.cs
│   └── Interfaces/
│       └── IAccountRepository.cs
│
├── HierarchyAccounts.Application/
│   ├── DTOs/
│   │   ├── AccountDto.cs
│   │   ├── AccountTreeDto.cs
│   │   ├── CreateAccountRequest.cs
│   │   └── MoveAccountRequest.cs
│   ├── Interfaces/
│   │   └── IAccountService.cs
│   └── Services/
│       └── AccountService.cs
│
├── HierarchyAccounts.Infrastructure/
│   ├── Data/
│   │   └── AppDbContext.cs
│   │   └── Migrations/         ← auto-generated, do not create manually
│   └── Repositories/
│       └── AccountRepository.cs
│
├── HierarchyAccounts.Api/
│   ├── Controllers/
│   │   └── AccountsController.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── HierarchyAccounts.Console/
│   ├── Services/
│   │   └── ApiClient.cs
│   ├── Display/
│   │   └── TreePrinter.cs
│   └── Program.cs
│
└── HierarchyAccounts.Tests/
    ├── Domain/
    │   └── AccountTests.cs
    └── Application/
        └── AccountServiceTests.cs
```

---

## PHASE 2: DOMAIN LAYER

### Step 2.1 — File: `HierarchyAccounts.Domain/Entities/Account.cs`

```csharp
namespace HierarchyAccounts.Domain.Entities;

/// <summary>
/// Represents a single node in the account hierarchy tree.
/// An account can be a global account, regional branch, country office, or local reseller.
/// </summary>
public class Account
{
    public const int MaxDepth = 5;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// Depth of this account in the tree. Root = 1, maximum = 5.
    /// </summary>
    public int Depth { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // EF Core navigation properties
    public Account? Parent { get; private set; }
    public ICollection<Account> Children { get; private set; } = new List<Account>();

    // Required by EF Core — do not use directly
    private Account() { }

    /// <summary>
    /// Creates a root account with no parent (depth = 1).
    /// </summary>
    public static Account CreateRoot(string name)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            Name = name,
            ParentId = null,
            Depth = 1,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a child account under the specified parent.
    /// Depth is automatically set to parent.Depth + 1.
    /// </summary>
    public static Account CreateChild(string name, Account parent)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            Name = name,
            ParentId = parent.Id,
            Depth = parent.Depth + 1,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Returns true if this account has no parent (i.e. is the root of the tree).
    /// </summary>
    public bool IsRoot() => ParentId == null;

    /// <summary>
    /// Updates the ParentId and Depth of this account.
    /// Called during a move operation before saving.
    /// </summary>
    public void SetParent(Guid? parentId, int newDepth)
    {
        ParentId = parentId;
        Depth = newDepth;
    }

    /// <summary>
    /// Recursively updates the depth of this account and all its loaded descendants.
    /// Must be called after a move to keep the entire subtree consistent.
    /// </summary>
    public void UpdateDepth(int newDepth)
    {
        Depth = newDepth;
        foreach (var child in Children)
            child.UpdateDepth(newDepth + 1);
    }

    public void Rename(string name) => Name = name;
}
```

### Step 2.2 — File: `HierarchyAccounts.Domain/Exceptions/DomainException.cs`

```csharp
namespace HierarchyAccounts.Domain.Exceptions;

/// <summary>
/// Base class for all domain-level business rule violations.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
```

### Step 2.3 — File: `HierarchyAccounts.Domain/Exceptions/CycleDetectedException.cs`

```csharp
namespace HierarchyAccounts.Domain.Exceptions;

/// <summary>
/// Thrown when a move operation would create a cycle in the hierarchy tree.
/// Example: attempting to move a parent account under one of its own descendants.
/// </summary>
public class CycleDetectedException : DomainException
{
    public CycleDetectedException()
        : base("The operation would create a cycle in the account hierarchy.") { }
}
```

### Step 2.4 — File: `HierarchyAccounts.Domain/Exceptions/MaxDepthExceededException.cs`

```csharp
namespace HierarchyAccounts.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would place an account deeper than the allowed maximum of 5 levels.
/// </summary>
public class MaxDepthExceededException : DomainException
{
    public MaxDepthExceededException()
        : base($"The operation would exceed the maximum allowed tree depth of {Entities.Account.MaxDepth}.") { }
}
```

### Step 2.5 — File: `HierarchyAccounts.Domain/Exceptions/RootAccountException.cs`

```csharp
namespace HierarchyAccounts.Domain.Exceptions;

/// <summary>
/// Thrown when a forbidden operation is attempted on the root account.
/// Forbidden operations: moving the root under another account, deleting the root.
/// </summary>
public class RootAccountException : DomainException
{
    public RootAccountException(string message) : base(message) { }
}
```

### Step 2.6 — File: `HierarchyAccounts.Domain/Interfaces/IAccountRepository.cs`

```csharp
namespace HierarchyAccounts.Domain.Interfaces;

using HierarchyAccounts.Domain.Entities;

/// <summary>
/// Contract for all persistence operations on Account entities.
/// Abstracts the database from the application layer.
/// </summary>
public interface IAccountRepository
{
    /// <summary>Returns a single account by ID, or null if not found.</summary>
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns an account with its direct Children collection loaded (one level deep).</summary>
    Task<Account?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the full subtree rooted at the given accountId, with all descendants loaded recursively.</summary>
    Task<Account?> GetSubtreeAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all accounts as a flat list. Used to build the full tree in memory.</summary>
    Task<List<Account>> GetFullTreeAsync(CancellationToken ct = default);

    /// <summary>Returns a flat list of all descendants (all levels) of the given account.</summary>
    Task<List<Account>> GetDescendantsAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Account account, CancellationToken ct = default);
    Task UpdateAsync(Account account, CancellationToken ct = default);
    Task DeleteAsync(Account account, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

---

## PHASE 3: APPLICATION LAYER

### Step 3.1 — File: `HierarchyAccounts.Application/DTOs/AccountDto.cs`

```csharp
namespace HierarchyAccounts.Application.DTOs;

/// <summary>
/// Flat representation of a single account. Used in single-account GET responses.
/// </summary>
public record AccountDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    int Depth,
    DateTime CreatedAt
);
```

### Step 3.2 — File: `HierarchyAccounts.Application/DTOs/AccountTreeDto.cs`

```csharp
namespace HierarchyAccounts.Application.DTOs;

/// <summary>
/// Hierarchical/recursive representation of an account and all its descendants.
/// Used for tree and subtree GET responses.
/// </summary>
public record AccountTreeDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    int Depth,
    DateTime CreatedAt,
    List<AccountTreeDto> Children
);
```

### Step 3.3 — File: `HierarchyAccounts.Application/DTOs/CreateAccountRequest.cs`

```csharp
namespace HierarchyAccounts.Application.DTOs;

/// <summary>
/// Request body for creating a new account.
/// Omit ParentId to create a root account (only one root may exist).
/// </summary>
public record CreateAccountRequest(
    string Name,
    Guid? ParentId
);
```

### Step 3.4 — File: `HierarchyAccounts.Application/DTOs/MoveAccountRequest.cs`

```csharp
namespace HierarchyAccounts.Application.DTOs;

/// <summary>
/// Request body for moving an account to a different parent.
/// </summary>
public record MoveAccountRequest(
    Guid NewParentId
);
```

### Step 3.5 — File: `HierarchyAccounts.Application/Interfaces/IAccountService.cs`

```csharp
namespace HierarchyAccounts.Application.Interfaces;

using HierarchyAccounts.Application.DTOs;

public interface IAccountService
{
    Task<AccountDto> CreateAsync(CreateAccountRequest request, CancellationToken ct = default);
    Task MoveAsync(Guid accountId, Guid newParentId, CancellationToken ct = default);
    Task DeleteAsync(Guid accountId, CancellationToken ct = default);
    Task<AccountDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AccountTreeDto> GetSubtreeAsync(Guid id, CancellationToken ct = default);
    Task<AccountTreeDto> GetFullTreeAsync(CancellationToken ct = default);
}
```

### Step 3.6 — File: `HierarchyAccounts.Application/Services/AccountService.cs`

Implement every method exactly as described below. All comments in English.

```csharp
namespace HierarchyAccounts.Application.Services;

using HierarchyAccounts.Application.DTOs;
using HierarchyAccounts.Application.Interfaces;
using HierarchyAccounts.Domain.Entities;
using HierarchyAccounts.Domain.Exceptions;
using HierarchyAccounts.Domain.Interfaces;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;

    public AccountService(IAccountRepository repository)
    {
        _repository = repository;
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────
    // Logic:
    // 1. If ParentId is null → create root account via Account.CreateRoot(name)
    // 2. If ParentId is provided → fetch parent (throw KeyNotFoundException if missing)
    // 3. Validate: parent.Depth + 1 > Account.MaxDepth → throw MaxDepthExceededException
    // 4. Create child: Account.CreateChild(name, parent)
    // 5. Persist and return AccountDto
    public async Task<AccountDto> CreateAsync(CreateAccountRequest request, CancellationToken ct = default)
    {
        Account account;

        if (request.ParentId is null)
        {
            account = Account.CreateRoot(request.Name);
        }
        else
        {
            var parent = await _repository.GetByIdAsync(request.ParentId.Value, ct)
                ?? throw new KeyNotFoundException($"Parent account with id '{request.ParentId}' was not found.");

            if (parent.Depth + 1 > Account.MaxDepth)
                throw new MaxDepthExceededException();

            account = Account.CreateChild(request.Name, parent);
        }

        await _repository.AddAsync(account, ct);
        await _repository.SaveChangesAsync(ct);

        return MapToDto(account);
    }

    // ── MoveAsync ────────────────────────────────────────────────────────────
    // Logic:
    // 1. Fetch the account to move (throw KeyNotFoundException if missing)
    // 2. If account.IsRoot() → throw RootAccountException
    // 3. Fetch newParent (throw KeyNotFoundException if missing)
    // 4. If newParent.Id == account.Id → throw CycleDetectedException (self-reference)
    // 5. Fetch all descendants of account (flat list)
    // 6. If descendants contain newParent.Id → throw CycleDetectedException
    // 7. Calculate new depth: newParent.Depth + 1
    // 8. Calculate depth delta: newDepth - account.Depth
    // 9. Find maximum depth among all descendants
    // 10. If (maxDescendantDepth + depthDelta) > Account.MaxDepth → throw MaxDepthExceededException
    // 11. Update account: account.SetParent(newParentId, newDepth)
    // 12. Update each descendant depth by depthDelta
    // 13. Persist all changes via a single SaveChangesAsync
    public async Task MoveAsync(Guid accountId, Guid newParentId, CancellationToken ct = default)
    {
        var account = await _repository.GetByIdAsync(accountId, ct)
            ?? throw new KeyNotFoundException($"Account with id '{accountId}' was not found.");

        if (account.IsRoot())
            throw new RootAccountException("The root account cannot be moved under another account.");

        var newParent = await _repository.GetByIdAsync(newParentId, ct)
            ?? throw new KeyNotFoundException($"Target parent account with id '{newParentId}' was not found.");

        if (newParent.Id == account.Id)
            throw new CycleDetectedException();

        var descendants = await _repository.GetDescendantsAsync(accountId, ct);

        if (descendants.Any(d => d.Id == newParentId))
            throw new CycleDetectedException();

        var newDepth = newParent.Depth + 1;
        var depthDelta = newDepth - account.Depth;

        if (descendants.Count > 0)
        {
            var maxDescendantDepth = descendants.Max(d => d.Depth);
            if (maxDescendantDepth + depthDelta > Account.MaxDepth)
                throw new MaxDepthExceededException();
        }
        else
        {
            if (newDepth > Account.MaxDepth)
                throw new MaxDepthExceededException();
        }

        account.SetParent(newParentId, newDepth);
        await _repository.UpdateAsync(account, ct);

        // Update depth for every descendant to keep the subtree consistent
        foreach (var descendant in descendants)
        {
            descendant.SetParent(descendant.ParentId, descendant.Depth + depthDelta);
            await _repository.UpdateAsync(descendant, ct);
        }

        await _repository.SaveChangesAsync(ct);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────
    // Logic:
    // 1. Fetch account with its direct children loaded (throw KeyNotFoundException if missing)
    // 2. If account.IsRoot() → throw RootAccountException (root deletion is forbidden)
    // 3. For each child: reassign child's parent to account.ParentId at account.Depth
    //    (children move up one level to the deleted account's parent)
    // 4. Update each child in the repository
    // 5. Delete the account
    // 6. SaveChangesAsync (single transaction)
    public async Task DeleteAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await _repository.GetByIdWithChildrenAsync(accountId, ct)
            ?? throw new KeyNotFoundException($"Account with id '{accountId}' was not found.");

        if (account.IsRoot())
            throw new RootAccountException("The root account cannot be deleted.");

        // Reassign children to the deleted account's parent
        foreach (var child in account.Children)
        {
            child.SetParent(account.ParentId, account.Depth);
            await _repository.UpdateAsync(child, ct);
        }

        await _repository.DeleteAsync(account, ct);
        await _repository.SaveChangesAsync(ct);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────
    public async Task<AccountDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Account with id '{id}' was not found.");

        return MapToDto(account);
    }

    // ── GetSubtreeAsync ───────────────────────────────────────────────────────
    public async Task<AccountTreeDto> GetSubtreeAsync(Guid id, CancellationToken ct = default)
    {
        var root = await _repository.GetSubtreeAsync(id, ct)
            ?? throw new KeyNotFoundException($"Account with id '{id}' was not found.");

        return MapToTreeDto(root);
    }

    // ── GetFullTreeAsync ──────────────────────────────────────────────────────
    // Loads all accounts, finds the root, and builds the tree in memory.
    public async Task<AccountTreeDto> GetFullTreeAsync(CancellationToken ct = default)
    {
        var allAccounts = await _repository.GetFullTreeAsync(ct);

        var root = allAccounts.FirstOrDefault(a => a.ParentId == null)
            ?? throw new KeyNotFoundException("No root account found in the system.");

        // Build a lookup of children grouped by ParentId for O(n) tree construction
        var childrenLookup = allAccounts
            .Where(a => a.ParentId.HasValue)
            .GroupBy(a => a.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return BuildTreeDto(root, childrenLookup);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static AccountDto MapToDto(Account account) => new(
        account.Id,
        account.Name,
        account.ParentId,
        account.Depth,
        account.CreatedAt
    );

    // Recursively maps an Account (with loaded Children) to AccountTreeDto
    private static AccountTreeDto MapToTreeDto(Account account) => new(
        account.Id,
        account.Name,
        account.ParentId,
        account.Depth,
        account.CreatedAt,
        account.Children.Select(MapToTreeDto).ToList()
    );

    // Builds AccountTreeDto from a flat list using a pre-built children lookup (O(n) complexity)
    private static AccountTreeDto BuildTreeDto(Account node, Dictionary<Guid, List<Account>> lookup)
    {
        var children = lookup.TryGetValue(node.Id, out var childList)
            ? childList.Select(c => BuildTreeDto(c, lookup)).ToList()
            : new List<AccountTreeDto>();

        return new AccountTreeDto(
            node.Id,
            node.Name,
            node.ParentId,
            node.Depth,
            node.CreatedAt,
            children
        );
    }
}
```

---

## PHASE 4: INFRASTRUCTURE LAYER

### Step 4.1 — File: `HierarchyAccounts.Infrastructure/Data/AppDbContext.cs`

```csharp
namespace HierarchyAccounts.Infrastructure.Data;

using HierarchyAccounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(a => a.Depth)
                  .IsRequired();

            entity.Property(a => a.CreatedAt)
                  .IsRequired();

            // Self-referencing relationship: each account optionally belongs to a parent account
            entity.HasOne(a => a.Parent)
                  .WithMany(a => a.Children)
                  .HasForeignKey(a => a.ParentId)
                  // Restrict cascade delete — child reassignment is handled in application code
                  .OnDelete(DeleteBehavior.Restrict);

            // Index for fast lookup of all children of a given parent
            entity.HasIndex(a => a.ParentId);

            // Index for depth-based filtering
            entity.HasIndex(a => a.Depth);
        });
    }
}
```

### Step 4.2 — File: `HierarchyAccounts.Infrastructure/Repositories/AccountRepository.cs`

```csharp
namespace HierarchyAccounts.Infrastructure.Repositories;

using HierarchyAccounts.Domain.Entities;
using HierarchyAccounts.Domain.Interfaces;
using HierarchyAccounts.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _context;

    public AccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Accounts.FindAsync(new object[] { id }, ct);

    public async Task<Account?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default)
        => await _context.Accounts
            .Include(a => a.Children)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    /// <summary>
    /// Loads all accounts into memory and reconstructs the full subtree rooted at the given id.
    /// This avoids complex recursive SQL and keeps the implementation simple and predictable.
    /// </summary>
    public async Task<Account?> GetSubtreeAsync(Guid id, CancellationToken ct = default)
    {
        var all = await _context.Accounts.ToListAsync(ct);
        return BuildTree(all, id);
    }

    public async Task<List<Account>> GetFullTreeAsync(CancellationToken ct = default)
        => await _context.Accounts.ToListAsync(ct);

    /// <summary>
    /// Returns a flat list of all descendants of the given account (all levels deep).
    /// Used for cycle detection and depth validation during move operations.
    /// </summary>
    public async Task<List<Account>> GetDescendantsAsync(Guid id, CancellationToken ct = default)
    {
        var all = await _context.Accounts.ToListAsync(ct);
        var result = new List<Account>();
        CollectDescendants(all, id, result);
        return result;
    }

    public async Task AddAsync(Account account, CancellationToken ct = default)
        => await _context.Accounts.AddAsync(account, ct);

    public Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        _context.Accounts.Update(account);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Account account, CancellationToken ct = default)
    {
        _context.Accounts.Remove(account);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds a tree of Account objects from a flat list by wiring up Children collections.
    /// Uses a dictionary for O(n) performance.
    /// </summary>
    private static Account? BuildTree(List<Account> all, Guid rootId)
    {
        // Group all accounts by their ParentId for fast child lookup
        var byParent = all
            .Where(a => a.ParentId.HasValue)
            .GroupBy(a => a.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var root = all.FirstOrDefault(a => a.Id == rootId);
        if (root is null) return null;

        AttachChildren(root, byParent);
        return root;
    }

    /// <summary>
    /// Recursively wires Children collections onto each Account node.
    /// </summary>
    private static void AttachChildren(Account node, Dictionary<Guid, List<Account>> byParent)
    {
        if (!byParent.TryGetValue(node.Id, out var children)) return;

        foreach (var child in children)
        {
            // Use reflection to set the private Children collection — EF Core normally handles this,
            // but since we are building the tree manually from a flat list we must wire it up ourselves.
            var childrenField = typeof(Account)
                .GetProperty(nameof(Account.Children))!;

            // Cast the existing collection and add child if not already present
            if (node.Children is List<Account> list && !list.Contains(child))
                list.Add(child);

            AttachChildren(child, byParent);
        }
    }

    /// <summary>
    /// Recursively collects all descendants of the given parentId into the result list.
    /// </summary>
    private static void CollectDescendants(List<Account> all, Guid parentId, List<Account> result)
    {
        var children = all.Where(a => a.ParentId == parentId).ToList();
        foreach (var child in children)
        {
            result.Add(child);
            CollectDescendants(all, child.Id, result);
        }
    }
}
```

> **Agent Note on `BuildTree`/`AttachChildren`:** The Children property on Account is declared as `ICollection<Account>` with a private setter initialised to `new List<Account>()`. Since EF Core normally populates this via change tracking, when building manually from a flat list you should work with the already-initialised list. Cast `node.Children` to `List<Account>` and call `.Add(child)` directly — no reflection needed. Adjust the implementation accordingly.

### Step 4.3 — Generate EF Core Migration

Run from the solution root **after** completing Steps 4.1 and 4.2:

```bash
dotnet ef migrations add InitialCreate \
  --project HierarchyAccounts.Infrastructure \
  --startup-project HierarchyAccounts.Api \
  --output-dir Data/Migrations

dotnet ef database update \
  --project HierarchyAccounts.Infrastructure \
  --startup-project HierarchyAccounts.Api
```

Expected migration output:
- Table `Accounts` with columns: `Id` (uniqueidentifier PK), `Name` (nvarchar(200)), `ParentId` (uniqueidentifier nullable FK), `Depth` (int), `CreatedAt` (datetime2)
- FK: `ParentId → Accounts.Id` with `ON DELETE NO ACTION` (Restrict)
- Indexes on `ParentId` and `Depth`

---

## PHASE 5: API LAYER

### Step 5.1 — File: `HierarchyAccounts.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HierarchyAccountsDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Step 5.2 — File: `HierarchyAccounts.Api/Program.cs`

```csharp
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
```

### Step 5.3 — File: `HierarchyAccounts.Api/Middleware/ExceptionHandlingMiddleware.cs`

```csharp
namespace HierarchyAccounts.Api.Middleware;

using HierarchyAccounts.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Global middleware that catches domain and application exceptions
/// and maps them to appropriate HTTP status codes with RFC 7807 ProblemDetails responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Map exception types to HTTP status codes
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException    => (StatusCodes.Status404NotFound,           "Resource not found."),
            CycleDetectedException  => (StatusCodes.Status400BadRequest,          "Cycle detected."),
            MaxDepthExceededException => (StatusCodes.Status400BadRequest,        "Maximum depth exceeded."),
            RootAccountException    => (StatusCodes.Status400BadRequest,          "Root account restriction."),
            DomainException         => (StatusCodes.Status400BadRequest,          "Business rule violation."),
            _                       => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        };

        return context.Response.WriteAsJsonAsync(problem);
    }
}
```

### Step 5.4 — File: `HierarchyAccounts.Api/Controllers/AccountsController.cs`

```csharp
namespace HierarchyAccounts.Api.Controllers;

using HierarchyAccounts.Application.DTOs;
using HierarchyAccounts.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Manages account hierarchy operations: create, move, delete, and retrieve accounts.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _service;

    public AccountsController(IAccountService service)
    {
        _service = service;
    }

    /// <summary>
    /// Creates a new account. Omit ParentId to create a root account.
    /// </summary>
    /// <param name="request">Account name and optional parent ID.</param>
    /// <returns>The newly created account.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Returns the details of a single account by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the full subtree rooted at the given account, as a nested hierarchy.
    /// </summary>
    [HttpGet("{id:guid}/subtree")]
    [ProducesResponseType(typeof(AccountTreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubtree(Guid id, CancellationToken ct)
    {
        var result = await _service.GetSubtreeAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the entire account hierarchy as a single nested tree starting from the root.
    /// </summary>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(AccountTreeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFullTree(CancellationToken ct)
    {
        var result = await _service.GetFullTreeAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Moves an account to a different parent. The root account cannot be moved.
    /// The move is rejected if it would create a cycle or exceed the maximum depth of 5.
    /// </summary>
    [HttpPatch("{id:guid}/move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveAccountRequest request, CancellationToken ct)
    {
        await _service.MoveAsync(id, request.NewParentId, ct);
        return NoContent();
    }

    /// <summary>
    /// Deletes an account. Its direct children are reassigned to the deleted account's parent.
    /// The root account cannot be deleted.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
```

---

## PHASE 6: CONSOLE APPLICATION

### Step 6.1 — Add required package to Console project

```bash
dotnet add HierarchyAccounts.Console package Microsoft.Extensions.Http
```

### Step 6.2 — File: `HierarchyAccounts.Console/Services/ApiClient.cs`

The console project uses its own local DTO copies (or reference the Application project). For simplicity, add a project reference to Application:

```bash
dotnet add HierarchyAccounts.Console/HierarchyAccounts.Console.csproj reference \
  HierarchyAccounts.Application/HierarchyAccounts.Application.csproj
```

```csharp
namespace HierarchyAccounts.Console.Services;

using System.Net.Http.Json;
using HierarchyAccounts.Application.DTOs;

/// <summary>
/// HTTP client wrapper for calling the Hierarchy Accounts REST API.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>
    /// Fetches the full account tree from GET /api/accounts/tree.
    /// </summary>
    public async Task<AccountTreeDto?> GetFullTreeAsync()
    {
        var response = await _http.GetAsync("api/accounts/tree");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountTreeDto>();
    }

    /// <summary>
    /// Fetches the subtree rooted at the given accountId from GET /api/accounts/{id}/subtree.
    /// </summary>
    public async Task<AccountTreeDto?> GetSubtreeAsync(Guid accountId)
    {
        var response = await _http.GetAsync($"api/accounts/{accountId}/subtree");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountTreeDto>();
    }
}
```

### Step 6.3 — File: `HierarchyAccounts.Console/Display/TreePrinter.cs`

```csharp
namespace HierarchyAccounts.Console.Display;

using HierarchyAccounts.Application.DTOs;

/// <summary>
/// Renders an AccountTreeDto as a readable ASCII tree in the terminal.
/// </summary>
/// <example>
/// Global Corp (depth: 1) [id: xxxxxxxx-...]
/// ├── Europe Region (depth: 2)
/// │   ├── Germany Office (depth: 3)
/// │   └── France Office (depth: 3)
/// └── Asia Region (depth: 2)
///     └── Japan Office (depth: 3)
/// </example>
public static class TreePrinter
{
    public static void Print(AccountTreeDto node, string prefix = "", bool isLast = true)
    {
        // Determine the connector character based on whether this is the last sibling
        var connector = prefix == "" ? "" : (isLast ? "└── " : "├── ");

        System.Console.WriteLine($"{prefix}{connector}{node.Name} (depth: {node.Depth}) [id: {node.Id}]");

        // Calculate prefix for child nodes
        var childPrefix = prefix + (prefix == "" ? "" : (isLast ? "    " : "│   "));

        for (int i = 0; i < node.Children.Count; i++)
        {
            var isLastChild = i == node.Children.Count - 1;
            Print(node.Children[i], childPrefix, isLastChild);
        }
    }
}
```

### Step 6.4 — File: `HierarchyAccounts.Console/Program.cs`

```csharp
// Console entry point for the GUHC Hierarchy Accounts viewer.
//
// Usage:
//   dotnet run                              → print the full account tree
//   dotnet run -- <guid>                    → print the subtree of the given account
//   dotnet run -- --api-url <url>           → override the default API base URL
//   dotnet run -- <guid> --api-url <url>   → combine both options

using HierarchyAccounts.Console.Display;
using HierarchyAccounts.Console.Services;

const string DefaultApiUrl = "https://localhost:7001/";

var apiUrl = DefaultApiUrl;
Guid? accountId = null;

// Parse CLI arguments
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--api-url" && i + 1 < args.Length)
    {
        apiUrl = args[++i];
    }
    else if (Guid.TryParse(args[i], out var parsedId))
    {
        accountId = parsedId;
    }
}

var client = new ApiClient(apiUrl);

try
{
    var tree = accountId.HasValue
        ? await client.GetSubtreeAsync(accountId.Value)
        : await client.GetFullTreeAsync();

    if (tree is null)
    {
        Console.Error.WriteLine("No data returned from the API.");
        Environment.Exit(1);
    }

    Console.WriteLine();
    Console.WriteLine(accountId.HasValue
        ? $"Subtree for account {accountId}:"
        : "Full Account Hierarchy:");
    Console.WriteLine(new string('─', 60));

    TreePrinter.Print(tree);
    Console.WriteLine();
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"[ERROR] Could not reach the API at {apiUrl}");
    Console.Error.WriteLine($"        {ex.Message}");
    Environment.Exit(1);
}
```

---

## PHASE 7: UNIT TESTS

### Step 7.1 — File: `HierarchyAccounts.Tests/Domain/AccountTests.cs`

```csharp
namespace HierarchyAccounts.Tests.Domain;

using FluentAssertions;
using HierarchyAccounts.Domain.Entities;

public class AccountTests
{
    // ── CreateRoot ────────────────────────────────────────────────────────────

    [Fact]
    public void CreateRoot_ShouldHaveDepthOne()
    {
        var account = Account.CreateRoot("Global Corp");
        account.Depth.Should().Be(1);
    }

    [Fact]
    public void CreateRoot_ShouldHaveNullParentId()
    {
        var account = Account.CreateRoot("Global Corp");
        account.ParentId.Should().BeNull();
    }

    [Fact]
    public void CreateRoot_ShouldGenerateNonEmptyId()
    {
        var account = Account.CreateRoot("Global Corp");
        account.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateRoot_ShouldSetNameCorrectly()
    {
        var account = Account.CreateRoot("Global Corp");
        account.Name.Should().Be("Global Corp");
    }

    // ── CreateChild ───────────────────────────────────────────────────────────

    [Fact]
    public void CreateChild_ShouldHaveDepthOfParentPlusOne()
    {
        var parent = Account.CreateRoot("Parent");
        var child = Account.CreateChild("Child", parent);
        child.Depth.Should().Be(2);
    }

    [Fact]
    public void CreateChild_ShouldSetParentIdToParentsId()
    {
        var parent = Account.CreateRoot("Parent");
        var child = Account.CreateChild("Child", parent);
        child.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public void CreateChild_ShouldHaveUniqueId()
    {
        var parent = Account.CreateRoot("Parent");
        var child1 = Account.CreateChild("Child1", parent);
        var child2 = Account.CreateChild("Child2", parent);
        child1.Id.Should().NotBe(child2.Id);
    }

    // ── IsRoot ────────────────────────────────────────────────────────────────

    [Fact]
    public void IsRoot_ShouldReturnTrue_WhenAccountHasNoParent()
    {
        var account = Account.CreateRoot("Root");
        account.IsRoot().Should().BeTrue();
    }

    [Fact]
    public void IsRoot_ShouldReturnFalse_WhenAccountHasParent()
    {
        var parent = Account.CreateRoot("Parent");
        var child = Account.CreateChild("Child", parent);
        child.IsRoot().Should().BeFalse();
    }

    // ── SetParent ─────────────────────────────────────────────────────────────

    [Fact]
    public void SetParent_ShouldUpdateParentIdAndDepth()
    {
        var account = Account.CreateRoot("Account");
        var newParentId = Guid.NewGuid();
        account.SetParent(newParentId, 3);

        account.ParentId.Should().Be(newParentId);
        account.Depth.Should().Be(3);
    }
}
```

### Step 7.2 — File: `HierarchyAccounts.Tests/Application/AccountServiceTests.cs`

```csharp
namespace HierarchyAccounts.Tests.Application;

using FluentAssertions;
using HierarchyAccounts.Application.DTOs;
using HierarchyAccounts.Application.Services;
using HierarchyAccounts.Domain.Entities;
using HierarchyAccounts.Domain.Exceptions;
using HierarchyAccounts.Domain.Interfaces;
using Moq;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _repoMock;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _repoMock = new Mock<IAccountRepository>();
        _service = new AccountService(_repoMock.Object);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithNoParent_ShouldCreateRootAccount()
    {
        var request = new CreateAccountRequest("Global Corp", null);

        var result = await _service.CreateAsync(request);

        result.ParentId.Should().BeNull();
        result.Depth.Should().Be(1);
        result.Name.Should().Be("Global Corp");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Account>(), default), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithParent_ShouldCreateChildAtCorrectDepth()
    {
        var parent = Account.CreateRoot("Root");
        _repoMock.Setup(r => r.GetByIdAsync(parent.Id, default)).ReturnsAsync(parent);

        var request = new CreateAccountRequest("Child", parent.Id);
        var result = await _service.CreateAsync(request);

        result.Depth.Should().Be(2);
        result.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task CreateAsync_WhenParentAtMaxDepth_ShouldThrow_MaxDepthExceededException()
    {
        // Build a chain at depth 5 (max)
        var root = Account.CreateRoot("Root");                       // depth 1
        var l2 = Account.CreateChild("L2", root);                   // depth 2
        var l3 = Account.CreateChild("L3", l2);                     // depth 3
        var l4 = Account.CreateChild("L4", l3);                     // depth 4
        var l5 = Account.CreateChild("L5", l4);                     // depth 5

        _repoMock.Setup(r => r.GetByIdAsync(l5.Id, default)).ReturnsAsync(l5);

        var request = new CreateAccountRequest("L6", l5.Id);

        await _service.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<MaxDepthExceededException>();
    }

    [Fact]
    public async Task CreateAsync_WhenParentNotFound_ShouldThrow_KeyNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Account?)null);

        var request = new CreateAccountRequest("Child", missingId);

        await _service.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── MoveAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task MoveAsync_WhenAccountIsRoot_ShouldThrow_RootAccountException()
    {
        var root = Account.CreateRoot("Root");
        var target = Account.CreateRoot("Other Root");

        _repoMock.Setup(r => r.GetByIdAsync(root.Id, default)).ReturnsAsync(root);

        await _service.Invoking(s => s.MoveAsync(root.Id, target.Id))
            .Should().ThrowAsync<RootAccountException>();
    }

    [Fact]
    public async Task MoveAsync_WhenNewParentIsSelf_ShouldThrow_CycleDetectedException()
    {
        var root = Account.CreateRoot("Root");
        var child = Account.CreateChild("Child", root);

        _repoMock.Setup(r => r.GetByIdAsync(child.Id, default)).ReturnsAsync(child);
        _repoMock.Setup(r => r.GetByIdAsync(child.Id, default)).ReturnsAsync(child); // newParent = self

        await _service.Invoking(s => s.MoveAsync(child.Id, child.Id))
            .Should().ThrowAsync<CycleDetectedException>();
    }

    [Fact]
    public async Task MoveAsync_WhenNewParentIsDescendant_ShouldThrow_CycleDetectedException()
    {
        var root = Account.CreateRoot("Root");
        var child = Account.CreateChild("Child", root);
        var grandchild = Account.CreateChild("Grandchild", child);

        _repoMock.Setup(r => r.GetByIdAsync(child.Id, default)).ReturnsAsync(child);
        _repoMock.Setup(r => r.GetByIdAsync(grandchild.Id, default)).ReturnsAsync(grandchild);
        // Descendants of child include grandchild
        _repoMock.Setup(r => r.GetDescendantsAsync(child.Id, default))
            .ReturnsAsync(new List<Account> { grandchild });

        await _service.Invoking(s => s.MoveAsync(child.Id, grandchild.Id))
            .Should().ThrowAsync<CycleDetectedException>();
    }

    [Fact]
    public async Task MoveAsync_WhenMoveExceedsMaxDepth_ShouldThrow_MaxDepthExceededException()
    {
        // Target parent is at depth 4; account has a child at depth 2 (relative).
        // After move: account → depth 5, its child → depth 6 (exceeds max).
        var root = Account.CreateRoot("Root");                 // depth 1
        var l2 = Account.CreateChild("L2", root);             // depth 2
        var l3 = Account.CreateChild("L3", l2);               // depth 3
        var l4 = Account.CreateChild("L4", l3);               // depth 4
        var movable = Account.CreateChild("Movable", root);   // depth 2 — will be moved
        var movableChild = Account.CreateChild("MovableChild", movable); // depth 3

        _repoMock.Setup(r => r.GetByIdAsync(movable.Id, default)).ReturnsAsync(movable);
        _repoMock.Setup(r => r.GetByIdAsync(l4.Id, default)).ReturnsAsync(l4);
        _repoMock.Setup(r => r.GetDescendantsAsync(movable.Id, default))
            .ReturnsAsync(new List<Account> { movableChild });

        // Moving movable (depth 2, child at depth 3) under l4 (depth 4):
        // movable → depth 5, movableChild → depth 6 → exceeds MaxDepth
        await _service.Invoking(s => s.MoveAsync(movable.Id, l4.Id))
            .Should().ThrowAsync<MaxDepthExceededException>();
    }

    [Fact]
    public async Task MoveAsync_WhenValid_ShouldUpdateAccountParentAndDepth()
    {
        var root = Account.CreateRoot("Root");
        var child = Account.CreateChild("Child", root);
        var newParent = Account.CreateChild("NewParent", root); // depth 2

        _repoMock.Setup(r => r.GetByIdAsync(child.Id, default)).ReturnsAsync(child);
        _repoMock.Setup(r => r.GetByIdAsync(newParent.Id, default)).ReturnsAsync(newParent);
        _repoMock.Setup(r => r.GetDescendantsAsync(child.Id, default))
            .ReturnsAsync(new List<Account>());

        await _service.MoveAsync(child.Id, newParent.Id);

        child.ParentId.Should().Be(newParent.Id);
        child.Depth.Should().Be(3); // newParent.Depth (2) + 1
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenAccountIsRoot_ShouldThrow_RootAccountException()
    {
        var root = Account.CreateRoot("Root");
        _repoMock.Setup(r => r.GetByIdWithChildrenAsync(root.Id, default)).ReturnsAsync(root);

        await _service.Invoking(s => s.DeleteAsync(root.Id))
            .Should().ThrowAsync<RootAccountException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenAccountNotFound_ShouldThrow_KeyNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdWithChildrenAsync(missingId, default))
            .ReturnsAsync((Account?)null);

        await _service.Invoking(s => s.DeleteAsync(missingId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenAccountHasNoChildren_ShouldDeleteDirectly()
    {
        var root = Account.CreateRoot("Root");
        var child = Account.CreateChild("Child", root);
        // child has no children loaded

        _repoMock.Setup(r => r.GetByIdWithChildrenAsync(child.Id, default)).ReturnsAsync(child);

        await _service.DeleteAsync(child.Id);

        _repoMock.Verify(r => r.DeleteAsync(child, default), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAccountHasChildren_ShouldReassignChildrenToGrandparent()
    {
        // Setup: root → parent → child
        // Deleting parent: child should be reassigned to root
        var root = Account.CreateRoot("Root");
        var parent = Account.CreateChild("Parent", root);   // depth 2
        var grandchild = Account.CreateChild("GrandChild", parent); // depth 3

        // Simulate EF Core loading the Children collection
        (parent.Children as List<Account>)!.Add(grandchild);

        _repoMock.Setup(r => r.GetByIdWithChildrenAsync(parent.Id, default)).ReturnsAsync(parent);

        await _service.DeleteAsync(parent.Id);

        // Grandchild should now point to root (parent's parent)
        grandchild.ParentId.Should().Be(root.Id);
        grandchild.Depth.Should().Be(2); // same depth as the deleted parent

        _repoMock.Verify(r => r.UpdateAsync(grandchild, default), Times.Once);
        _repoMock.Verify(r => r.DeleteAsync(parent, default), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
```

---

## PHASE 8: GIT COMMIT STRATEGY

Perform one commit per step in the following order. Keep commits small and focused.

```bash
git init
git add .gitignore  # add a standard .NET .gitignore first
git commit -m "chore: initialize repository with .gitignore"

git add HierarchyAccounts.sln */*.csproj
git commit -m "chore: initialize solution structure with all projects and references"

git add HierarchyAccounts.Domain/
git commit -m "feat(domain): add Account entity with factory methods and depth logic"

git commit -m "feat(domain): add domain exceptions (CycleDetected, MaxDepth, RootAccount)"

git commit -m "feat(domain): add IAccountRepository interface"

git add HierarchyAccounts.Application/
git commit -m "feat(application): add DTOs and IAccountService interface"

git commit -m "feat(application): implement AccountService with full business logic"

git add HierarchyAccounts.Infrastructure/Data/AppDbContext.cs
git commit -m "feat(infrastructure): add AppDbContext with EF Core model configuration"

git add HierarchyAccounts.Infrastructure/Repositories/
git commit -m "feat(infrastructure): implement AccountRepository"

# After running dotnet ef migrations add:
git add HierarchyAccounts.Infrastructure/Data/Migrations/
git commit -m "feat(infrastructure): add EF Core initial migration"

git add HierarchyAccounts.Api/Middleware/
git commit -m "feat(api): add global ExceptionHandlingMiddleware"

git add HierarchyAccounts.Api/Controllers/
git commit -m "feat(api): add AccountsController with all REST endpoints"

git add HierarchyAccounts.Api/Program.cs HierarchyAccounts.Api/appsettings.json
git commit -m "feat(api): configure Program.cs with DI, Swagger and middleware pipeline"

git add HierarchyAccounts.Console/
git commit -m "feat(console): add ApiClient, TreePrinter and CLI entry point"

git add HierarchyAccounts.Tests/
git commit -m "test: add domain unit tests for Account entity"

git commit -m "test: add application unit tests for AccountService"

git add README.md
git commit -m "docs: add README with setup, usage and example API calls"
```

---

## PHASE 9: README.md

Create this file in the solution root:

````markdown
# GUHC Hierarchy Accounts System

A backend service for managing account hierarchies for Grand Unified Holding Corp.
Built with .NET 8, ASP.NET Core Web API, and MS SQL Server.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local install or Docker)
- EF Core CLI tools:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Database Setup

1. Update the connection string in `HierarchyAccounts.Api/appsettings.json`:
   ```json
   "DefaultConnection": "Server=localhost;Database=HierarchyAccountsDb;Trusted_Connection=True;TrustServerCertificate=True;"
   ```

2. Apply migrations:
   ```bash
   dotnet ef database update \
     --project HierarchyAccounts.Infrastructure \
     --startup-project HierarchyAccounts.Api
   ```

## Running the API

```bash
cd HierarchyAccounts.Api
dotnet run
```

Swagger UI is available at: `https://localhost:7001/swagger`

## Running the Console App

```bash
cd HierarchyAccounts.Console

# Print the full account tree
dotnet run

# Print the subtree of a specific account
dotnet run -- <accountId>

# Use a custom API URL
dotnet run -- --api-url https://myhost:7001/

# Combine options
dotnet run -- <accountId> --api-url https://myhost:7001/
```

## Running Tests

```bash
dotnet test
```

## API Reference

| Method   | Endpoint                        | Description                                          |
|----------|---------------------------------|------------------------------------------------------|
| `POST`   | `/api/accounts`                 | Create a new account (root if no ParentId given)     |
| `GET`    | `/api/accounts/{id}`            | Get details of a single account                      |
| `GET`    | `/api/accounts/{id}/subtree`    | Get the subtree rooted at an account (nested JSON)   |
| `GET`    | `/api/accounts/tree`            | Get the full account hierarchy (nested JSON)         |
| `PATCH`  | `/api/accounts/{id}/move`       | Move an account under a new parent                   |
| `DELETE` | `/api/accounts/{id}`            | Delete account; children are reassigned to its parent|

## Business Rules

- Maximum tree depth: **5 levels**
- Cycles are **never allowed** (validated on every create and move)
- The **root account cannot be moved or deleted**
- Deleting an account **reassigns its direct children** to the deleted account's parent
- Depth constraint is enforced on move for the **entire subtree**, not just the moved node

## Example Data Flow

```http
# 1. Create root account (depth 1)
POST /api/accounts
{ "name": "Global Corp" }

# 2. Create regional branch (depth 2)
POST /api/accounts
{ "name": "Europe Region", "parentId": "<globalCorpId>" }

# 3. Create country office (depth 3)
POST /api/accounts
{ "name": "Germany Office", "parentId": "<europeId>" }

# 4. View full hierarchy
GET /api/accounts/tree

# 5. Move Germany under a different region
PATCH /api/accounts/<germanyId>/move
{ "newParentId": "<asiaId>" }

# 6. Delete Europe (Germany, now under Asia, is unaffected)
DELETE /api/accounts/<europeId>
```

## Project Structure

```
HierarchyAccounts.Domain         → Entities, domain exceptions, repository interface
HierarchyAccounts.Application    → DTOs, service interface, business logic
HierarchyAccounts.Infrastructure → EF Core DbContext, repository implementation, migrations
HierarchyAccounts.Api            → ASP.NET Core controllers, middleware, Swagger config
HierarchyAccounts.Console        → CLI viewer using the REST API
HierarchyAccounts.Tests          → xUnit unit tests (domain + application layer)
```
````

---

## CRITICAL IMPLEMENTATION RULES

The AI agent **must** follow all of the rules below without exception:

1. **All code comments are in English** — no other language anywhere in the codebase.
2. **Private setters on all Entity properties** — EF Core accesses them via reflection; the domain model must protect its own invariants.
3. **`DeleteBehavior.Restrict` on the self-referencing FK** — SQL Server must never cascade-delete children automatically; all reassignment logic lives in `AccountService.DeleteAsync`.
4. **`CancellationToken` in every async method signature** — required for proper ASP.NET Core request cancellation.
5. **`KeyNotFoundException` for 404 cases** — the `ExceptionHandlingMiddleware` maps this to HTTP 404 automatically.
6. **`record` types for all DTOs** — immutability and value equality by default.
7. **Cycle detection uses `GetDescendantsAsync`** — never rely solely on DB constraints for cycle prevention.
8. **Depth is computed in memory** — load the flat list from the DB, then calculate depths in C#; do not write recursive SQL.
9. **Single `SaveChangesAsync` at the end of each service method** — Unit of Work pattern; never call it mid-operation.
10. **Swagger XML `<summary>` tags on all controller actions** — required for full OpenAPI documentation.
11. **`dotnet-ef` must be installed globally** before running migration commands.
12. **The `Children` property is initialised to `new List<Account>()`** in the entity — cast to `List<Account>` when populating manually in the repository; do not use reflection.
13. **No `Program.cs` top-level `using` statements in the test project** — use full namespace declarations or file-scoped namespaces consistently.
14. **The console project must reference the Application project** to reuse `AccountTreeDto` — do not duplicate DTO definitions.
