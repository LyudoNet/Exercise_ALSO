namespace HierarchyAccounts.Api.Controllers;

using HierarchyAccounts.Application.DTOs;
using HierarchyAccounts.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Manages account hierarchy operations: create, move, delete, and retrieve accounts.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _service;

    public AccountsController(IAccountService service)
    {
        _service = service;
    }

    /// <summary>
    /// Creates a new account. Omit ParentId to create a root account.
    /// </summary>
    /// <param name="request">Account name and optional parent ID.</param>
    /// <returns>The newly created account.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Returns the details of a single account by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the full subtree rooted at the given account, as a nested hierarchy.
    /// </summary>
    [HttpGet("{id:guid}/subtree")]
    [ProducesResponseType(typeof(AccountTreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubtree(Guid id, CancellationToken ct)
    {
        var result = await _service.GetSubtreeAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the entire account hierarchy as a single nested tree starting from the root.
    /// </summary>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(AccountTreeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFullTree(CancellationToken ct)
    {
        var result = await _service.GetFullTreeAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Moves an account to a different parent. The root account cannot be moved.
    /// The move is rejected if it would create a cycle or exceed the maximum depth of 5.
    /// </summary>
    [HttpPatch("{id:guid}/move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveAccountRequest request, CancellationToken ct)
    {
        await _service.MoveAsync(id, request.NewParentId, ct);
        return NoContent();
    }

    /// <summary>
    /// Deletes an account. Its direct children are reassigned to the deleted account's parent.
    /// The root account cannot be deleted.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
