namespace HierarchyAccounts.Tests.Domain;

using FluentAssertions;
using HierarchyAccounts.Domain.Entities;

public class AccountTests
{


    [Fact]
    public void CreateRoot_ShouldHaveDepthOne()
    {
        var account = Account.CreateRoot("Global Corp");
        account.Depth.Should().Be(1);
    }

    [Fact]
    public void CreateRoot_ShouldHaveNullParentId()
    {
        var account = Account.CreateRoot("Global Corp");
        account.ParentId.Should().BeNull();
    }

    [Fact]
    public void CreateRoot_ShouldGenerateNonEmptyId()
    {
        var account = Account.CreateRoot("Global Corp");
        account.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateRoot_ShouldSetNameCorrectly()
    {
        var account = Account.CreateRoot("Global Corp");
        account.Name.Should().Be("Global Corp");
    }



    [Fact]
    public void CreateChild_ShouldHaveDepthOfParentPlusOne()
    {
        var parent = Account.CreateRoot("Parent");
        var child = Account.CreateChild("Child", parent);
        child.Depth.Should().Be(2);
    }

    [Fact]
    public void CreateChild_ShouldSetParentIdToParentsId()
    {
        var parent = Account.CreateRoot("Parent");
        var child = Account.CreateChild("Child", parent);
        child.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public void CreateChild_ShouldHaveUniqueId()
    {
        var parent = Account.CreateRoot("Parent");
        var child1 = Account.CreateChild("Child1", parent);
        var child2 = Account.CreateChild("Child2", parent);
        child1.Id.Should().NotBe(child2.Id);
    }



    [Fact]
    public void IsRoot_ShouldReturnTrue_WhenAccountHasNoParent()
    {
        var account = Account.CreateRoot("Root");
        account.IsRoot().Should().BeTrue();
    }

    [Fact]
    public void IsRoot_ShouldReturnFalse_WhenAccountHasParent()
    {
        var parent = Account.CreateRoot("Parent");
        var child = Account.CreateChild("Child", parent);
        child.IsRoot().Should().BeFalse();
    }



    [Fact]
    public void SetParent_ShouldUpdateParentIdAndDepth()
    {
        var account = Account.CreateRoot("Account");
        var newParentId = Guid.NewGuid();
        account.SetParent(newParentId, 3);

        account.ParentId.Should().Be(newParentId);
        account.Depth.Should().Be(3);
    }
}
