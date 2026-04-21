using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Reports;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IReportPdfGenerator _reportPdfGenerator;

    public ReportsController(IReportService reportService, IReportPdfGenerator reportPdfGenerator)
    {
        _reportService = reportService;
        _reportPdfGenerator = reportPdfGenerator;
    }

    [HttpGet("{type}/pdf")]
    public async Task<IActionResult> DownloadPdf(string type, [FromQuery] ReportPdfFilterRequest request, CancellationToken cancellationToken)
    {
        if (request.DateFrom.HasValue && request.DateTo.HasValue && request.DateFrom > request.DateTo)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "Validation failed",
                new Dictionary<string, string[]>
                {
                    [nameof(request.DateFrom)] = ["DateFrom cannot be later than DateTo."]
                },
                HttpContext.TraceIdentifier));
        }

        var normalizedType = type.Trim().ToLowerInvariant();

        return normalizedType switch
        {
            "students" => await BuildStudentsPdfAsync(request, cancellationToken),
            "attendance" => await BuildAttendancePdfAsync(request, cancellationToken),
            "fees" => await BuildFeesPdfAsync(request, cancellationToken),
            _ => BadRequest(ApiResponse<object>.Fail("Unsupported report type.", traceId: HttpContext.TraceIdentifier))
        };
    }

    private async Task<FileContentResult> BuildStudentsPdfAsync(ReportPdfFilterRequest request, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetStudentsReportAsync(request, cancellationToken);
        var pdf = _reportPdfGenerator.GenerateStudentsReport(report);
        return File(pdf, "application/pdf", BuildFileName("students"));
    }

    private async Task<FileContentResult> BuildAttendancePdfAsync(ReportPdfFilterRequest request, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetAttendanceReportAsync(request, cancellationToken);
        var pdf = _reportPdfGenerator.GenerateAttendanceReport(report);
        return File(pdf, "application/pdf", BuildFileName("attendance"));
    }

    private async Task<FileContentResult> BuildFeesPdfAsync(ReportPdfFilterRequest request, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetFeesReportAsync(request, cancellationToken);
        var pdf = _reportPdfGenerator.GenerateFeesReport(report);
        return File(pdf, "application/pdf", BuildFileName("fees"));
    }

    private static string BuildFileName(string type)
    {
        return $"school-management-{type}-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";
    }
}
