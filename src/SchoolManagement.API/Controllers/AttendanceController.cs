using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Students;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public sealed class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IStudentService _studentService;

    public AttendanceController(IAttendanceService attendanceService, IStudentService studentService)
    {
        _attendanceService = attendanceService;
        _studentService = studentService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<PagedResponse<AttendanceResponse>>>> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<PagedResponse<AttendanceResponse>>.Ok(await _attendanceService.GetForTeacherUserAsync(GetCurrentUserId(), request, cancellationToken)));
        }

        return Ok(ApiResponse<PagedResponse<AttendanceResponse>>.Ok(await _attendanceService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<AttendanceResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<AttendanceResponse>.Ok(await _attendanceService.GetForTeacherUserByIdAsync(GetCurrentUserId(), id, cancellationToken)));
        }

        return Ok(ApiResponse<AttendanceResponse>.Ok(await _attendanceService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpGet("student/{studentId:guid}")]
    [Authorize(Roles = "Admin,Teacher,Student,Parent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceResponse>>>> GetByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Student"))
        {
            var currentStudent = await _studentService.GetByUserIdAsync(GetCurrentUserId(), cancellationToken);
            if (currentStudent.Id != studentId)
            {
                return Forbid();
            }
        }

        if (User.IsInRole("Parent"))
        {
            var children = await _studentService.GetByParentUserIdAsync(GetCurrentUserId(), new PaginationRequest { PageNumber = 1, PageSize = 100 }, cancellationToken);
            if (!children.Items.Any(x => x.Id == studentId))
            {
                return Forbid();
            }
        }

        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<IReadOnlyCollection<AttendanceResponse>>.Ok(await _attendanceService.GetForTeacherUserByStudentIdAsync(GetCurrentUserId(), studentId, cancellationToken)));
        }

        return Ok(ApiResponse<IReadOnlyCollection<AttendanceResponse>>.Ok(await _attendanceService.GetByStudentIdAsync(studentId, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<AttendanceResponse>>> Create(CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        var response = User.IsInRole("Teacher")
            ? await _attendanceService.CreateForTeacherUserAsync(GetCurrentUserId(), request, cancellationToken)
            : await _attendanceService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<AttendanceResponse>.Ok(response, "Attendance created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<AttendanceResponse>>> Update(Guid id, UpdateAttendanceRequest request, CancellationToken cancellationToken)
    {
        var response = User.IsInRole("Teacher")
            ? await _attendanceService.UpdateForTeacherUserAsync(GetCurrentUserId(), id, request, cancellationToken)
            : await _attendanceService.UpdateAsync(id, request, cancellationToken);

        return Ok(ApiResponse<AttendanceResponse>.Ok(response, "Attendance updated successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }
}
