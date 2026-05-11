namespace HierarchyAccounts.Domain.Exceptions;

/// <summary>
/// Thrown when a move operation would create a cycle in the hierarchy tree.
/// Example: attempting to move a parent account under one of its own descendants.
/// </summary>
public class CycleDetectedException : DomainException
{
    public CycleDetectedException()
        : base("The operation would create a cycle in the account hierarchy.") { }
}
