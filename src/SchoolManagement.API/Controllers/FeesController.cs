using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Fees;
using SchoolManagement.Application.Students;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class FeesController : ControllerBase
{
    private readonly IFeeService _feeService;
    private readonly IStudentService _studentService;

    public FeesController(IFeeService feeService, IStudentService studentService)
    {
        _feeService = feeService;
        _studentService = studentService;
    }

    [HttpGet("fees")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<FeeResponse>>>> GetAll([FromQuery] FeeFilterRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResponse<FeeResponse>>.Ok(await _feeService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("fees/{id:guid}")]
    [Authorize(Roles = "Admin,Student,Parent")]
    public async Task<ActionResult<ApiResponse<FeeResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var fee = await _feeService.GetByIdAsync(id, cancellationToken);
        if (!await CanAccessStudentAsync(fee.StudentId, cancellationToken))
        {
            return Forbid();
        }

        return Ok(ApiResponse<FeeResponse>.Ok(fee));
    }

    [HttpGet("fees/student/{studentId:guid}")]
    [Authorize(Roles = "Admin,Student,Parent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<FeeResponse>>>> GetByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        if (!await CanAccessStudentAsync(studentId, cancellationToken))
        {
            return Forbid();
        }

        return Ok(ApiResponse<IReadOnlyCollection<FeeResponse>>.Ok(await _feeService.GetByStudentIdAsync(studentId, cancellationToken)));
    }

    [HttpPost("fees")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<FeeResponse>>> Create(CreateFeeRequest request, CancellationToken cancellationToken)
    {
        var response = await _feeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<FeeResponse>.Ok(response, "Fee created successfully."));
    }

    [HttpPut("fees/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<FeeResponse>>> Update(Guid id, UpdateFeeRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<FeeResponse>.Ok(await _feeService.UpdateAsync(id, request, cancellationToken), "Fee updated successfully."));
    }

    [HttpDelete("fees/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _feeService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Fee deleted successfully."));
    }

    [HttpGet("payments")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<PaymentResponse>>>> GetPayments([FromQuery] PaymentFilterRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResponse<PaymentResponse>>.Ok(await _feeService.GetPaymentsPagedAsync(request, cancellationToken)));
    }

    [HttpPost("payments")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> AddPayment(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await _feeService.AddPaymentAsync(request, cancellationToken);
        return Ok(ApiResponse<PaymentResponse>.Ok(response, "Payment recorded successfully."));
    }

    private async Task<bool> CanAccessStudentAsync(Guid studentId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
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

    /// <summary>
    /// Admin-only: scans for fees past their due date and sends overdue reminder notifications
    /// to each affected student and their parent. Returns the number of overdue fees processed.
    /// </summary>
    [HttpPost("fees/notify-overdue")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> NotifyOverdue(CancellationToken cancellationToken)
    {
        var count = await _feeService.NotifyOverdueFeesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, $"Overdue notifications sent for {count} fee{(count == 1 ? "" : "s")}."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }
}
