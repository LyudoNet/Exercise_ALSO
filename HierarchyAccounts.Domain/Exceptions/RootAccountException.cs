namespace HierarchyAccounts.Domain.Exceptions;

/// <summary>
/// Thrown when a forbidden operation is attempted on the root account.
/// Forbidden operations: moving the root under another account, deleting the root.
/// </summary>
public class RootAccountException : DomainException
{
    public RootAccountException(string message) : base(message) { }
}
