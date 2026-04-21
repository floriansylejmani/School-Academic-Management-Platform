namespace SchoolManagement.Application.Analytics;

// ---------------------------------------------------------------------------
// KPI snapshot
// ---------------------------------------------------------------------------

public sealed record KpiResponse(
    int TotalStudents,
    int TotalTeachers,
    int TotalClasses,
    double AttendanceRate,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    int UnpaidFeesCount,
    decimal TotalCollectedPayments,
    int RecentNotificationsCount,
    double ExamPassRate,
    double ExamAverageScore);

// ---------------------------------------------------------------------------
// Attendance trends  (per-day breakdown)
// ---------------------------------------------------------------------------

public sealed record AttendanceTrendPoint(
    string Date,
    int Present,
    int Absent,
    int Late,
    int Excused);

public sealed record AttendanceTrendsResponse(
    IReadOnlyCollection<AttendanceTrendPoint> Trends,
    int DaysRequested);

// ---------------------------------------------------------------------------
// Exam performance  (per-exam averages + pass/fail split)
// ---------------------------------------------------------------------------

public sealed record ExamPerformanceItem(
    string ExamTitle,
    string SubjectName,
    string ClassName,
    double AverageScore,
    decimal TotalMarks,
    int PassCount,
    int FailCount,
    int TotalSubmissions);

public sealed record ExamPerformanceResponse(
    IReadOnlyCollection<ExamPerformanceItem> ExamAverages,
    double OverallPassRate,
    double OverallAverageScore,
    int TotalExamsWithResults);

// ---------------------------------------------------------------------------
// Finance summary  (fee status breakdown + collected payments)
// ---------------------------------------------------------------------------

public sealed record FinanceSummaryResponse(
    decimal TotalFeesAmount,
    decimal PaidAmount,
    decimal PendingAmount,
    decimal OverdueAmount,
    decimal PartiallyPaidAmount,
    int PaidCount,
    int PendingCount,
    int OverdueCount,
    int PartiallyPaidCount,
    decimal TotalCollectedPayments,
    int TotalPaymentsCount);

// ---------------------------------------------------------------------------
// Service interface
// ---------------------------------------------------------------------------

public interface IAnalyticsService
{
    Task<KpiResponse> GetKpisAsync(CancellationToken cancellationToken);
    Task<AttendanceTrendsResponse> GetAttendanceTrendsAsync(int days, CancellationToken cancellationToken);
    Task<ExamPerformanceResponse> GetExamPerformanceAsync(Guid? classId, CancellationToken cancellationToken);
    Task<FinanceSummaryResponse> GetFinanceSummaryAsync(CancellationToken cancellationToken);
}
