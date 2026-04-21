namespace SchoolManagement.Application.Reports;

public sealed class ReportPdfFilterRequest
{
    public Guid? ClassId { get; init; }
    public Guid? StudentId { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
}

public sealed record ReportMetadata(
    string Title,
    string? ClassName,
    string? StudentName,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    DateTime GeneratedAtUtc);

public sealed record StudentReportRow(
    string StudentName,
    string StudentCode,
    string Email,
    string ClassName,
    string? ParentName,
    DateOnly AdmissionDate,
    decimal AttendancePercentage,
    decimal AverageScorePercentage,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal OutstandingBalance,
    int ResultCount);

public sealed record StudentsPdfReportData(
    ReportMetadata Metadata,
    IReadOnlyCollection<StudentReportRow> Students);

public sealed record AttendanceReportRow(
    string StudentName,
    string ClassName,
    string SubjectName,
    string TeacherName,
    DateOnly Date,
    string Status,
    string? Remarks);

public sealed record AttendancePdfReportData(
    ReportMetadata Metadata,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    IReadOnlyCollection<AttendanceReportRow> Records);

public sealed record FeeReportRow(
    string StudentName,
    string ClassName,
    string FeeType,
    decimal Amount,
    decimal PaidAmount,
    decimal OutstandingBalance,
    DateOnly DueDate,
    string Status);

public sealed record FeesPdfReportData(
    ReportMetadata Metadata,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal TotalOutstanding,
    IReadOnlyCollection<FeeReportRow> Fees);

public interface IReportService
{
    Task<StudentsPdfReportData> GetStudentsReportAsync(ReportPdfFilterRequest request, CancellationToken cancellationToken);
    Task<AttendancePdfReportData> GetAttendanceReportAsync(ReportPdfFilterRequest request, CancellationToken cancellationToken);
    Task<FeesPdfReportData> GetFeesReportAsync(ReportPdfFilterRequest request, CancellationToken cancellationToken);
}

public interface IReportPdfGenerator
{
    byte[] GenerateStudentsReport(StudentsPdfReportData report);
    byte[] GenerateAttendanceReport(AttendancePdfReportData report);
    byte[] GenerateFeesReport(FeesPdfReportData report);
}
