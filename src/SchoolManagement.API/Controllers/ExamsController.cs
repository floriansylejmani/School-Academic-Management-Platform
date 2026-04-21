using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Exams;
using SchoolManagement.Application.Students;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/exams")]
[Authorize]
public sealed class ExamsController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly IStudentService _studentService;

    public ExamsController(IExamService examService, IStudentService studentService)
    {
        _examService = examService;
        _studentService = studentService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher,Student")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ExamResponse>>>> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<PagedResponse<ExamResponse>>.Ok(await _examService.GetForTeacherUserAsync(GetCurrentUserId(), request, cancellationToken)));
        }

        return Ok(ApiResponse<PagedResponse<ExamResponse>>.Ok(await _examService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher,Student,Parent")]
    public async Task<ActionResult<ApiResponse<ExamResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessExamAsync(id, cancellationToken))
        {
            return Forbid();
        }

        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<ExamResponse>.Ok(await _examService.GetForTeacherUserByIdAsync(GetCurrentUserId(), id, cancellationToken)));
        }

        return Ok(ApiResponse<ExamResponse>.Ok(await _examService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpGet("class/{classId:guid}")]
    [Authorize(Roles = "Admin,Teacher,Student,Parent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ExamResponse>>>> GetByClass(Guid classId, CancellationToken cancellationToken)
    {
        if (!await CanAccessClassAsync(classId, cancellationToken))
        {
            return Forbid();
        }

        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<IReadOnlyCollection<ExamResponse>>.Ok(await _examService.GetForTeacherUserByClassIdAsync(GetCurrentUserId(), classId, cancellationToken)));
        }

        return Ok(ApiResponse<IReadOnlyCollection<ExamResponse>>.Ok(await _examService.GetByClassIdAsync(classId, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<ExamResponse>>> Create(CreateExamRequest request, CancellationToken cancellationToken)
    {
        var response = User.IsInRole("Teacher")
            ? await _examService.CreateForTeacherUserAsync(GetCurrentUserId(), request, cancellationToken)
            : await _examService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<ExamResponse>.Ok(response, "Exam created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<ExamResponse>>> Update(Guid id, UpdateExamRequest request, CancellationToken cancellationToken)
    {
        var response = User.IsInRole("Teacher")
            ? await _examService.UpdateForTeacherUserAsync(GetCurrentUserId(), id, request, cancellationToken)
            : await _examService.UpdateAsync(id, request, cancellationToken);

        return Ok(ApiResponse<ExamResponse>.Ok(response, "Exam updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            await _examService.DeleteForTeacherUserAsync(GetCurrentUserId(), id, cancellationToken);
        }
        else
        {
            await _examService.DeleteAsync(id, cancellationToken);
        }

        return Ok(ApiResponse<object>.Ok(null, "Exam deleted successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }

    private async Task<bool> CanAccessExamAsync(Guid examId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Teacher"))
        {
            return true;
        }

        var exam = await _examService.GetByIdAsync(examId, cancellationToken);
        return await CanAccessClassAsync(exam.ClassId, cancellationToken);
    }

    private async Task<bool> CanAccessClassAsync(Guid classId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Teacher"))
        {
            return true;
        }

        if (User.IsInRole("Student"))
        {
            var currentStudent = await _studentService.GetByUserIdAsync(GetCurrentUserId(), cancellationToken);
            return currentStudent.ClassId == classId;
        }

        if (User.IsInRole("Parent"))
        {
            var children = await _studentService.GetByParentUserIdAsync(
                GetCurrentUserId(),
                new PaginationRequest { PageNumber = 1, PageSize = 100 },
                cancellationToken);

            return children.Items.Any(x => x.ClassId == classId);
        }

        return false;
    }
}
