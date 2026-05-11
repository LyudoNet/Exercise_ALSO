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
