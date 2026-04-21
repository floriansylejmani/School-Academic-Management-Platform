using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Timetable;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/timetable")]
[Authorize]
public sealed class TimetableController : ControllerBase
{
    private readonly ITimetableService _timetableService;

    public TimetableController(ITimetableService timetableService)
    {
        _timetableService = timetableService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TimetableEntryResponse>>>> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<PagedResponse<TimetableEntryResponse>>.Ok(await _timetableService.GetForTeacherUserAsync(GetCurrentUserId(), request, cancellationToken)));
        }

        return Ok(ApiResponse<PagedResponse<TimetableEntryResponse>>.Ok(await _timetableService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("class/{classId:guid}")]
    [Authorize(Roles = "Admin,Teacher,Student,Parent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TimetableEntryResponse>>>> GetByClass(Guid classId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<IReadOnlyCollection<TimetableEntryResponse>>.Ok(await _timetableService.GetForTeacherUserByClassIdAsync(GetCurrentUserId(), classId, cancellationToken)));
        }

        return Ok(ApiResponse<IReadOnlyCollection<TimetableEntryResponse>>.Ok(await _timetableService.GetByClassIdAsync(classId, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<TimetableEntryResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<TimetableEntryResponse>.Ok(await _timetableService.GetForTeacherUserByIdAsync(GetCurrentUserId(), id, cancellationToken)));
        }

        return Ok(ApiResponse<TimetableEntryResponse>.Ok(await _timetableService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TimetableEntryResponse>>> Create(CreateTimetableEntryRequest request, CancellationToken cancellationToken)
    {
        var response = await _timetableService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<TimetableEntryResponse>.Ok(response, "Timetable entry created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TimetableEntryResponse>>> Update(Guid id, UpdateTimetableEntryRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<TimetableEntryResponse>.Ok(await _timetableService.UpdateAsync(id, request, cancellationToken), "Timetable entry updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _timetableService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Timetable entry deleted successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }
}
