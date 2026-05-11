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
    /// Children is initialised to new List&lt;Account&gt;() on the entity, so we cast and add directly.
    /// </summary>
    private static void AttachChildren(Account node, Dictionary<Guid, List<Account>> byParent)
    {
        if (!byParent.TryGetValue(node.Id, out var children)) return;

        var list = (List<Account>)node.Children;
        foreach (var child in children)
        {
            if (!list.Contains(child))
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
