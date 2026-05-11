namespace HierarchyAccounts.Application.DTOs;

/// <summary>
/// Request body for creating a new account.
/// Omit ParentId to create a root account (only one root may exist).
/// </summary>
public record CreateAccountRequest(
    string Name,
    Guid? ParentId
);
