namespace HierarchyAccounts.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would place an account deeper than the allowed maximum of 5 levels.
/// </summary>
public class MaxDepthExceededException : DomainException
{
    public MaxDepthExceededException()
        : base($"The operation would exceed the maximum allowed tree depth of {Entities.Account.MaxDepth}.") { }
}
