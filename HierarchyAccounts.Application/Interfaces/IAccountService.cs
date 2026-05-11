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
