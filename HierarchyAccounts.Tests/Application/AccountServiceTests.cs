namespace HierarchyAccounts.Tests.Application;

using FluentAssertions;
using HierarchyAccounts.Application.DTOs;
using HierarchyAccounts.Application.Services;
using HierarchyAccounts.Domain.Entities;
using HierarchyAccounts.Domain.Exceptions;
using HierarchyAccounts.Domain.Interfaces;
using Moq;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _repoMock;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _repoMock = new Mock<IAccountRepository>();
        _service = new AccountService(_repoMock.Object);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithNoParent_ShouldCreateRootAccount()
    {
        var request = new CreateAccountRequest("Global Corp", null);

        var result = await _service.CreateAsync(request);

        result.ParentId.Should().BeNull();
        result.Depth.Should().Be(1);
        result.Name.Should().Be("Global Corp");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Account>(), default), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithParent_ShouldCreateChildAtCorrectDepth()
    {
        var parent = Account.CreateRoot("Root");
        _repoMock.Setup(r => r.GetByIdAsync(parent.Id, default)).ReturnsAsync(parent);

        var request = new CreateAccountRequest("Child", parent.Id);
        var result = await _service.CreateAsync(request);

        result.Depth.Should().Be(2);
        result.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task CreateAsync_WhenParentAtMaxDepth_ShouldThrow_MaxDepthExceededException()
    {
        // Build a chain at depth 5 (max)
        var root = Account.CreateRoot("Root");                       // depth 1
        var l2 = Account.CreateChild("L2", root);                   // depth 2
        var l3 = Account.CreateChild("L3", l2);                     // depth 3
        var l4 = Account.CreateChild("L4", l3);                     // depth 4
        var l5 = Account.CreateChild("L5", l4);                     // depth 5

        _repoMock.Setup(r => r.GetByIdAsync(l5.Id, default)).ReturnsAsync(l5);

        var request = new CreateAccountRequest("L6", l5.Id);

        await _service.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<MaxDepthExceededException>();
    }

    [Fact]
    public async Task CreateAsync_WhenParentNotFound_ShouldThrow_KeyNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Account?)null);

        var request = new CreateAccountRequest("Child", missingId);

        await _service.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── MoveAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task MoveAsync_WhenAccountIsRoot_ShouldThrow_RootAccountException()
    {
        var root = Account.CreateRoot("Root");
        var target = Account.CreateRoot("Other Root");

        _repoMock.Setup(r => r.GetByIdAsync(root.Id, default)).ReturnsAsync(root);

        await _service.Invoking(s => s.MoveAsync(root.Id, target.Id))
            .Should().ThrowAsync<RootAccountException>();
    }

    [Fact]
    public async Task MoveAsync_WhenNewParentIsSelf_ShouldThrow_CycleDetectedException()
    {
        var root = Account.CreateRoot("Root");
        var child = Account.CreateChild("Child", root);

        _repoMock.Setup(r => r.GetByIdAsync(child.Id, default)).ReturnsAsync(child);
        _repoMock.Setup(r => r.GetByIdAsync(child.Id, default)).ReturnsAsync(child); // newParent = self

        await _service.Invoking(s => s.MoveAsync(child.Id, child.Id))
            .Should().ThrowAsync<CycleDetectedException>();
    }

    [Fact]
    public async Task MoveAsync_WhenNewParentIsDescendant_ShouldThrow_CycleDetectedException()
    {
        var root = Account.CreateRoot("Root");
        var child = Account.CreateChild("Child", root);
        var grandchild = Account.CreateChild("Grandchild", child);

        _repoMock.Setup(r => r.GetByIdAsync(child.Id, default)).ReturnsAsync(child);
        _repoMock.Setup(r => r.GetByIdAsync(grandchild.Id, default)).ReturnsAsync(grandchild);
        // Descendants of child include grandchild
        _repoMock.Setup(r => r.GetDescendantsAsync(child.Id, default))
            .ReturnsAsync(new List<Account> { grandchild });

        await _service.Invoking(s => s.MoveAsync(child.Id, grandchild.Id))
            .Should().ThrowAsync<CycleDetectedException>();
    }

    [Fact]
    public async Task MoveAsync_WhenMoveExceedsMaxDepth_ShouldThrow_MaxDepthExceededException()
    {
        // Target parent is at depth 4; account has a child at depth 2 (relative).
        // After move: account → depth 5, its child → depth 6 (exceeds max).
        var root = Account.CreateRoot("Root");                 // depth 1
        var l2 = Account.CreateChild("L2", root);             // depth 2
        var l3 = Account.CreateChild("L3", l2);               // depth 3
        var l4 = Account.CreateChild("L4", l3);               // depth 4
        var movable = Account.CreateChild("Movable", root);   // depth 2 — will be moved
        var movableChild = Account.CreateChild("MovableChild", movable); // depth 3

        _repoMock.Setup(r => r.GetByIdAsync(movable.Id, default)).ReturnsAsync(movable);
        _repoMock.Setup(r => r.GetByIdAsync(l4.Id, default)).ReturnsAsync(l4);
        _repoMock.Setup(r => r.GetDescendantsAsync(movable.Id, default))
            .ReturnsAsync(new List<Account> { movableChild });

        // Moving movable (depth 2, child at depth 3) under l4 (depth 4):
        // movable → depth 5, movableChild → depth 6 → exceeds MaxDepth
        await _service.Invoking(s => s.MoveAsync(movable.Id, l4.Id))
            .Should().ThrowAsync<MaxDepthExceededException>();
    }

    [Fact]
    public async Task MoveAsync_WhenValid_ShouldUpdateAccountParentAndDepth()
    {
        var root = Account.CreateRoot("Root");
        var child = Account.CreateChild("Child", root);
        var newParent = Account.CreateChild("NewParent", root); // depth 2

        _repoMock.Setup(r => r.GetByIdAsync(child.Id, default)).ReturnsAsync(child);
        _repoMock.Setup(r => r.GetByIdAsync(newParent.Id, default)).ReturnsAsync(newParent);
        _repoMock.Setup(r => r.GetDescendantsAsync(child.Id, default))
            .ReturnsAsync(new List<Account>());

        await _service.MoveAsync(child.Id, newParent.Id);

        child.ParentId.Should().Be(newParent.Id);
        child.Depth.Should().Be(3); // newParent.Depth (2) + 1
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenAccountIsRoot_ShouldThrow_RootAccountException()
    {
        var root = Account.CreateRoot("Root");
        _repoMock.Setup(r => r.GetByIdWithChildrenAsync(root.Id, default)).ReturnsAsync(root);

        await _service.Invoking(s => s.DeleteAsync(root.Id))
            .Should().ThrowAsync<RootAccountException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenAccountNotFound_ShouldThrow_KeyNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdWithChildrenAsync(missingId, default))
            .ReturnsAsync((Account?)null);

        await _service.Invoking(s => s.DeleteAsync(missingId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenAccountHasNoChildren_ShouldDeleteDirectly()
    {
        var root = Account.CreateRoot("Root");
        var child = Account.CreateChild("Child", root);
        // child has no children loaded

        _repoMock.Setup(r => r.GetByIdWithChildrenAsync(child.Id, default)).ReturnsAsync(child);

        await _service.DeleteAsync(child.Id);

        _repoMock.Verify(r => r.DeleteAsync(child, default), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAccountHasChildren_ShouldReassignChildrenToGrandparent()
    {
        // Setup: root → parent → child
        // Deleting parent: child should be reassigned to root
        var root = Account.CreateRoot("Root");
        var parent = Account.CreateChild("Parent", root);   // depth 2
        var grandchild = Account.CreateChild("GrandChild", parent); // depth 3

        // Simulate EF Core loading the Children collection
        (parent.Children as List<Account>)!.Add(grandchild);

        _repoMock.Setup(r => r.GetByIdWithChildrenAsync(parent.Id, default)).ReturnsAsync(parent);

        await _service.DeleteAsync(parent.Id);

        // Grandchild should now point to root (parent's parent)
        grandchild.ParentId.Should().Be(root.Id);
        grandchild.Depth.Should().Be(2); // same depth as the deleted parent

        _repoMock.Verify(r => r.UpdateAsync(grandchild, default), Times.Once);
        _repoMock.Verify(r => r.DeleteAsync(parent, default), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
