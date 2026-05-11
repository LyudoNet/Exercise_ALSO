namespace HierarchyAccounts.Domain.Entities;

/// <summary> Node in the account hierarchy tree. </summary>
public class Account
{
    public const int MaxDepth = 5;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }

    /// <summary> Depth (Root = 1, Max = 5). </summary>
    public int Depth { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // EF Core navigation properties
    public Account? Parent { get; private set; }
    public ICollection<Account> Children { get; private set; } = new List<Account>();

    // Required by EF Core — do not use directly
    private Account() { }

    /// <summary> Creates a root account (depth = 1). </summary>
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

    /// <summary> Creates a child account (Depth = parent.Depth + 1). </summary>
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

    /// <summary> Returns true if this is the root. </summary>
    public bool IsRoot() => ParentId == null;

    /// <summary> Updates ParentId and Depth. </summary>
    public void SetParent(Guid? parentId, int newDepth)
    {
        ParentId = parentId;
        Depth = newDepth;
    }

    /// <summary> Recursively updates subtree depth. </summary>
    public void UpdateDepth(int newDepth)
    {
        Depth = newDepth;
        foreach (var child in Children)
            child.UpdateDepth(newDepth + 1);
    }

    public void Rename(string name) => Name = name;
}
