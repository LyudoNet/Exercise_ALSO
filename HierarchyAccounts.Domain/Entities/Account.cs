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
