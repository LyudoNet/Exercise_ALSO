namespace HierarchyAccounts.Application.DTOs;

/// <summary>
/// Request body for moving an account to a different parent.
/// </summary>
public record MoveAccountRequest(
    Guid NewParentId
);
