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
