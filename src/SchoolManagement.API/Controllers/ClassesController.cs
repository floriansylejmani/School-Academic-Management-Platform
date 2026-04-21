using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/classes")]
[Authorize]
public sealed class ClassesController : ControllerBase
{
    private readonly IClassService _classService;

    public ClassesController(IClassService classService)
    {
        _classService = classService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ClassResponse>>>> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<PagedResponse<ClassResponse>>.Ok(await _classService.GetForTeacherUserAsync(GetCurrentUserId(), request, cancellationToken)));
        }

        return Ok(ApiResponse<PagedResponse<ClassResponse>>.Ok(await _classService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<ClassResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<ClassResponse>.Ok(await _classService.GetForTeacherUserByIdAsync(GetCurrentUserId(), id, cancellationToken)));
        }

        return Ok(ApiResponse<ClassResponse>.Ok(await _classService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ClassResponse>>> Create(CreateClassRequest request, CancellationToken cancellationToken)
    {
        var response = await _classService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<ClassResponse>.Ok(response, "Class created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ClassResponse>>> Update(Guid id, UpdateClassRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<ClassResponse>.Ok(await _classService.UpdateAsync(id, request, cancellationToken), "Class updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _classService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Class deleted successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }
}
