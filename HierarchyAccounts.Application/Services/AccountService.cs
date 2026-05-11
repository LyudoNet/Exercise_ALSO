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

    /// <summary>
    /// Creates a new account.
    /// </summary>
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

    /// <summary>
    /// Moves an account to a new parent.
    /// </summary>
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

    /// <summary>
    /// Deletes an account and reassigns its children to its parent.
    /// </summary>
    public async Task DeleteAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await _repository.GetByIdWithChildrenAsync(accountId, ct)
            ?? throw new KeyNotFoundException($"Account with id '{accountId}' was not found.");

        if (account.IsRoot())
            throw new RootAccountException("The root account cannot be deleted.");

        // Reassign children to the deleted account's parent
        foreach (var child in account.Children.ToList())
        {
            child.SetParent(account.ParentId, account.Depth);
            await _repository.UpdateAsync(child, ct);
        }

        await _repository.DeleteAsync(account, ct);
        await _repository.SaveChangesAsync(ct);
    }

    public async Task<AccountDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Account with id '{id}' was not found.");

        return MapToDto(account);
    }

    public async Task<AccountTreeDto> GetSubtreeAsync(Guid id, CancellationToken ct = default)
    {
        var root = await _repository.GetSubtreeAsync(id, ct)
            ?? throw new KeyNotFoundException($"Account with id '{id}' was not found.");

        return MapToTreeDto(root);
    }

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
