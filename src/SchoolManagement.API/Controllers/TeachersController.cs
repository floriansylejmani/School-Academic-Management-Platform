using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Teachers;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/teachers")]
[Authorize]
public sealed class TeachersController : ControllerBase
{
    private readonly ITeacherService _teacherService;

    public TeachersController(ITeacherService teacherService)
    {
        _teacherService = teacherService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TeacherResponse>>>> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResponse<TeacherResponse>>.Ok(await _teacherService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<TeacherResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            var currentTeacher = await _teacherService.GetByUserIdAsync(GetCurrentUserId(), cancellationToken);
            if (currentTeacher.Id != id)
            {
                return Forbid();
            }
        }

        return Ok(ApiResponse<TeacherResponse>.Ok(await _teacherService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpGet("me")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<ApiResponse<TeacherResponse>>> GetMe(CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<TeacherResponse>.Ok(await _teacherService.GetByUserIdAsync(GetCurrentUserId(), cancellationToken)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TeacherResponse>>> Create(CreateTeacherRequest request, CancellationToken cancellationToken)
    {
        var response = await _teacherService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<TeacherResponse>.Ok(response, "Teacher created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TeacherResponse>>> Update(Guid id, UpdateTeacherRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<TeacherResponse>.Ok(await _teacherService.UpdateAsync(id, request, cancellationToken), "Teacher updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _teacherService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Teacher deleted successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }
}
