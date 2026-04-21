using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Results;
using SchoolManagement.Application.Students;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/results")]
[Authorize]
public sealed class ResultsController : ControllerBase
{
    private readonly IResultService _resultService;
    private readonly IStudentService _studentService;

    public ResultsController(IResultService resultService, IStudentService studentService)
    {
        _resultService = resultService;
        _studentService = studentService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ResultResponse>>>> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<PagedResponse<ResultResponse>>.Ok(await _resultService.GetForTeacherUserAsync(GetCurrentUserId(), request, cancellationToken)));
        }

        return Ok(ApiResponse<PagedResponse<ResultResponse>>.Ok(await _resultService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher,Student,Parent")]
    public async Task<ActionResult<ApiResponse<ResultResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<ResultResponse>.Ok(await _resultService.GetForTeacherUserByIdAsync(GetCurrentUserId(), id, cancellationToken)));
        }

        return Ok(ApiResponse<ResultResponse>.Ok(await _resultService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpGet("student/{studentId:guid}")]
    [Authorize(Roles = "Admin,Teacher,Student,Parent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ResultResponse>>>> GetByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        if (!await CanAccessStudentAsync(studentId, cancellationToken))
        {
            return Forbid();
        }

        if (User.IsInRole("Teacher"))
        {
            return Ok(ApiResponse<IReadOnlyCollection<ResultResponse>>.Ok(await _resultService.GetForTeacherUserByStudentIdAsync(GetCurrentUserId(), studentId, cancellationToken)));
        }

        return Ok(ApiResponse<IReadOnlyCollection<ResultResponse>>.Ok(await _resultService.GetByStudentIdAsync(studentId, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<ResultResponse>>> Create(CreateResultRequest request, CancellationToken cancellationToken)
    {
        var response = User.IsInRole("Teacher")
            ? await _resultService.CreateForTeacherUserAsync(GetCurrentUserId(), request, cancellationToken)
            : await _resultService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<ResultResponse>.Ok(response, "Result created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<ResultResponse>>> Update(Guid id, UpdateResultRequest request, CancellationToken cancellationToken)
    {
        var response = User.IsInRole("Teacher")
            ? await _resultService.UpdateForTeacherUserAsync(GetCurrentUserId(), id, request, cancellationToken)
            : await _resultService.UpdateAsync(id, request, cancellationToken);

        return Ok(ApiResponse<ResultResponse>.Ok(response, "Result updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher"))
        {
            await _resultService.DeleteForTeacherUserAsync(GetCurrentUserId(), id, cancellationToken);
        }
        else
        {
            await _resultService.DeleteAsync(id, cancellationToken);
        }

        return Ok(ApiResponse<object>.Ok(null, "Result deleted successfully."));
    }

    private async Task<bool> CanAccessStudentAsync(Guid studentId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        if (User.IsInRole("Teacher"))
        {
            return await _studentService.CanTeacherAccessStudentAsync(GetCurrentUserId(), studentId, cancellationToken);
        }

        if (User.IsInRole("Student"))
        {
            var currentStudent = await _studentService.GetByUserIdAsync(GetCurrentUserId(), cancellationToken);
            return currentStudent.Id == studentId;
        }

        if (User.IsInRole("Parent"))
        {
            var children = await _studentService.GetByParentUserIdAsync(GetCurrentUserId(), new PaginationRequest { PageNumber = 1, PageSize = 100 }, cancellationToken);
            return children.Items.Any(x => x.Id == studentId);
        }

        return false;
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }
}
