using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Analytics;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Admin")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Returns a KPI snapshot: student/teacher/class counts, attendance rate,
    /// exam pass rate, fee counts, total collected payments, and recent notifications.
    /// </summary>
    [HttpGet("kpis")]
    public async Task<ActionResult<ApiResponse<KpiResponse>>> GetKpis(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/analytics/kpis requested.");
        var result = await _analyticsService.GetKpisAsync(cancellationToken);
        return Ok(ApiResponse<KpiResponse>.Ok(result));
    }

    /// <summary>
    /// Returns per-day attendance breakdown (present/absent/late/excused) for
    /// the last <paramref name="days"/> days (clamped 7–90, default 30).
    /// </summary>
    [HttpGet("attendance-trends")]
    public async Task<ActionResult<ApiResponse<AttendanceTrendsResponse>>> GetAttendanceTrends(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GET /api/analytics/attendance-trends requested. Days: {Days}", days);
        var result = await _analyticsService.GetAttendanceTrendsAsync(days, cancellationToken);
        return Ok(ApiResponse<AttendanceTrendsResponse>.Ok(result));
    }

    /// <summary>
    /// Returns per-exam average score, pass count, and fail count.
    /// Optionally filtered to a single class via <paramref name="classId"/>.
    /// Only exams that have at least one result are included.
    /// </summary>
    [HttpGet("exam-performance")]
    public async Task<ActionResult<ApiResponse<ExamPerformanceResponse>>> GetExamPerformance(
        [FromQuery] Guid? classId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GET /api/analytics/exam-performance requested. ClassId: {ClassId}", classId);
        var result = await _analyticsService.GetExamPerformanceAsync(classId, cancellationToken);
        return Ok(ApiResponse<ExamPerformanceResponse>.Ok(result));
    }

    /// <summary>
    /// Returns a full finance snapshot: fee status breakdown (Paid / Pending /
    /// Overdue / PartiallyPaid) by count and amount, plus total collected payments.
    /// </summary>
    [HttpGet("finance-summary")]
    public async Task<ActionResult<ApiResponse<FinanceSummaryResponse>>> GetFinanceSummary(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/analytics/finance-summary requested.");
        var result = await _analyticsService.GetFinanceSummaryAsync(cancellationToken);
        return Ok(ApiResponse<FinanceSummaryResponse>.Ok(result));
    }

    /// <summary>
    /// Simple test endpoint to verify routing works with authorization
    /// </summary>
    [HttpGet("test")]
    public ActionResult TestEndpoint()
    {
        return Ok(new { message = "Analytics test endpoint works!", timestamp = DateTime.UtcNow });
    }
}
