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
