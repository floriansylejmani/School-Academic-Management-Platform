using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Students;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/students")]
[Authorize]
public sealed class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StudentResponse>>>> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<PagedResponse<StudentResponse>>.Ok(await _studentService.GetForTeacherUserAsync(GetCurrentUserId(), request, cancellationToken)));
        }

        return Ok(ApiResponse<PagedResponse<StudentResponse>>.Ok(await _studentService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<ApiResponse<StudentResponse>>> GetMe(CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<StudentResponse>.Ok(await _studentService.GetByUserIdAsync(GetCurrentUserId(), cancellationToken)));
    }

    [HttpGet("parent/me")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StudentResponse>>>> GetMyChildren([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResponse<StudentResponse>>.Ok(await _studentService.GetByParentUserIdAsync(GetCurrentUserId(), request, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher,Student")]
    public async Task<ActionResult<ApiResponse<StudentResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<StudentResponse>.Ok(await _studentService.GetForTeacherUserByIdAsync(GetCurrentUserId(), id, cancellationToken)));
        }

        if (User.IsInRole("Student"))
        {
            var currentStudent = await _studentService.GetByUserIdAsync(GetCurrentUserId(), cancellationToken);
            if (currentStudent.Id != id)
            {
                return Forbid();
            }
        }

        return Ok(ApiResponse<StudentResponse>.Ok(await _studentService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<StudentResponse>>> Create(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var response = await _studentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<StudentResponse>.Ok(response, "Student created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<StudentResponse>>> Update(Guid id, UpdateStudentRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<StudentResponse>.Ok(await _studentService.UpdateAsync(id, request, cancellationToken), "Student updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _studentService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Student deleted successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }
}
