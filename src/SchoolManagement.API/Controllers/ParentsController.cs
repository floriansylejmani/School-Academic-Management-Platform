using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Parents;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/parents")]
[Authorize]
public sealed class ParentsController : ControllerBase
{
    private readonly IParentService _parentService;

    public ParentsController(IParentService parentService)
    {
        _parentService = parentService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ParentResponse>>>> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResponse<ParentResponse>>.Ok(await _parentService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Parent")]
    public async Task<ActionResult<ApiResponse<ParentResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Parent"))
        {
            var currentParent = await _parentService.GetByUserIdAsync(GetCurrentUserId(), cancellationToken);
            if (currentParent.Id != id)
            {
                return Forbid();
            }
        }

        return Ok(ApiResponse<ParentResponse>.Ok(await _parentService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ParentResponse>>> Create(CreateParentRequest request, CancellationToken cancellationToken)
    {
        var response = await _parentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<ParentResponse>.Ok(response, "Parent created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ParentResponse>>> Update(Guid id, UpdateParentRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<ParentResponse>.Ok(await _parentService.UpdateAsync(id, request, cancellationToken), "Parent updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _parentService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Parent deleted successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }
}
