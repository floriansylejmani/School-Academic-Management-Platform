using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Subjects;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/subjects")]
[Authorize]
public sealed class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<PagedResponse<SubjectResponse>>>> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<PagedResponse<SubjectResponse>>.Ok(await _subjectService.GetForTeacherUserAsync(GetCurrentUserId(), request, cancellationToken)));
        }

        return Ok(ApiResponse<PagedResponse<SubjectResponse>>.Ok(await _subjectService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<SubjectResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<SubjectResponse>.Ok(await _subjectService.GetForTeacherUserByIdAsync(GetCurrentUserId(), id, cancellationToken)));
        }

        return Ok(ApiResponse<SubjectResponse>.Ok(await _subjectService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SubjectResponse>>> Create(CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var response = await _subjectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<SubjectResponse>.Ok(response, "Subject created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SubjectResponse>>> Update(Guid id, UpdateSubjectRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<SubjectResponse>.Ok(await _subjectService.UpdateAsync(id, request, cancellationToken), "Subject updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _subjectService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Subject deleted successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }
}
