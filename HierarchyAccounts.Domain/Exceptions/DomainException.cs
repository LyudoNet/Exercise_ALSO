namespace HierarchyAccounts.Domain.Exceptions;

/// <summary>
/// Base class for all domain-level business rule violations.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
