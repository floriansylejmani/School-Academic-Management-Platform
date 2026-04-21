using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Submissions;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/submissions")]
[Authorize]
public sealed class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;

    public SubmissionsController(ISubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher,Student")]
    public async Task<ActionResult<ApiResponse<PagedResponse<SubmissionResponse>>>> GetAll([FromQuery] SubmissionQueryRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<PagedResponse<SubmissionResponse>>.Ok(
                await _submissionService.GetForTeacherUserAsync(GetCurrentUserId(), request, cancellationToken)));
        }

        if (User.IsInRole("Student"))
        {
            return Ok(ApiResponse<PagedResponse<SubmissionResponse>>.Ok(
                await _submissionService.GetForStudentUserAsync(GetCurrentUserId(), request, cancellationToken)));
        }

        return Ok(ApiResponse<PagedResponse<SubmissionResponse>>.Ok(
            await _submissionService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher,Student")]
    public async Task<ActionResult<ApiResponse<SubmissionResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<SubmissionResponse>.Ok(
                await _submissionService.GetForTeacherUserByIdAsync(GetCurrentUserId(), id, cancellationToken)));
        }

        if (User.IsInRole("Student"))
        {
            return Ok(ApiResponse<SubmissionResponse>.Ok(
                await _submissionService.GetForStudentUserByIdAsync(GetCurrentUserId(), id, cancellationToken)));
        }

        return Ok(ApiResponse<SubmissionResponse>.Ok(
            await _submissionService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher,Student")]
    public async Task<ActionResult<ApiResponse<SubmissionResponse>>> Create([FromBody] CreateSubmissionRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        SubmissionResponse response;

        if (User.IsInRole("Student"))
        {
            response = await _submissionService.CreateForStudentUserAsync(currentUserId, request, cancellationToken);
        }
        else if (User.IsInRole("Teacher"))
        {
            response = await _submissionService.CreateForTeacherUserAsync(currentUserId, request, cancellationToken);
        }
        else
        {
            response = await _submissionService.CreateAsync(currentUserId, request, cancellationToken);
        }

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<SubmissionResponse>.Ok(response, "Submission created successfully."));
    }

    [HttpPut("{id:guid}/teacher-review")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<SubmissionResponse>>> UpdateTeacherReview(Guid id, [FromBody] UpdateSubmissionTeacherReviewRequest request, CancellationToken cancellationToken)
    {
        var response = User.IsInRole("Teacher")
            ? await _submissionService.UpdateTeacherReviewForTeacherUserAsync(GetCurrentUserId(), id, request, cancellationToken)
            : await _submissionService.UpdateTeacherReviewAsync(GetCurrentUserId(), id, request, cancellationToken);

        return Ok(ApiResponse<SubmissionResponse>.Ok(response, "Teacher review updated successfully."));
    }

    [HttpPost("{id:guid}/ai-feedback")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<SubmissionResponse>>> GenerateAIFeedback(Guid id, [FromBody] RequestSubmissionAIRequest request, CancellationToken cancellationToken)
    {
        var response = User.IsInRole("Teacher")
            ? await _submissionService.GenerateAIFeedbackForTeacherUserAsync(GetCurrentUserId(), id, request, cancellationToken)
            : await _submissionService.GenerateAIFeedbackAsync(GetCurrentUserId(), id, request, cancellationToken);

        return Ok(ApiResponse<SubmissionResponse>.Ok(response, "AI feedback generated successfully."));
    }

    [HttpPost("{id:guid}/smart-grade")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<SubmissionResponse>>> GenerateSmartGrade(Guid id, [FromBody] RequestSubmissionAIRequest request, CancellationToken cancellationToken)
    {
        var response = User.IsInRole("Teacher")
            ? await _submissionService.GenerateSmartGradeForTeacherUserAsync(GetCurrentUserId(), id, request, cancellationToken)
            : await _submissionService.GenerateSmartGradeAsync(GetCurrentUserId(), id, request, cancellationToken);

        return Ok(ApiResponse<SubmissionResponse>.Ok(response, "Smart grade guidance generated successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }
}
