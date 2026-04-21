using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Analytics;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Persistence.Services;

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(AppDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // KPI snapshot
    // -----------------------------------------------------------------------

    public async Task<KpiResponse> GetKpisAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching analytics KPI snapshot.");

        var thirtyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // ── Counts ──────────────────────────────────────────────────────────
        var totalStudents = await _context.Students.CountAsync(cancellationToken);
        var totalTeachers = await _context.Teachers.CountAsync(cancellationToken);
        var totalClasses = await _context.AcademicClasses.CountAsync(cancellationToken);

        // ── Attendance (last 30 days, grouped by status in a single query) ──
        var attendanceByStatus = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(x => x.Date >= thirtyDaysAgo)
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var presentCount = attendanceByStatus.FirstOrDefault(x => x.Status == AttendanceStatus.Present)?.Count ?? 0;
        var absentCount  = attendanceByStatus.FirstOrDefault(x => x.Status == AttendanceStatus.Absent)?.Count ?? 0;
        var lateCount    = attendanceByStatus.FirstOrDefault(x => x.Status == AttendanceStatus.Late)?.Count ?? 0;
        var excusedCount = attendanceByStatus.FirstOrDefault(x => x.Status == AttendanceStatus.Excused)?.Count ?? 0;
        var totalAttendance = presentCount + absentCount + lateCount + excusedCount;

        // Present + Late both count as "attended"
        var attendanceRate = totalAttendance == 0
            ? 0.0
            : Math.Round((double)(presentCount + lateCount) / totalAttendance * 100, 1);

        // ── Fees ─────────────────────────────────────────────────────────────
        var unpaidFeesCount = await _context.Fees
            .CountAsync(x => x.Status != FeeStatus.Paid, cancellationToken);

        // ── Payments collected ───────────────────────────────────────────────
        var totalCollectedPayments = await _context.Payments
            .SumAsync(x => (decimal?)x.AmountPaid, cancellationToken) ?? 0m;

        // ── Notifications (last 7 days) ──────────────────────────────────────
        var recentNotificationsCount = await _context.Notifications
            .CountAsync(x => x.CreatedAt >= sevenDaysAgo, cancellationToken);

        // ── Exam results — pass rate + average score (both DB-side) ──────────
        var resultProjections = await (
            from r in _context.Results.AsNoTracking()
            join e in _context.Exams on r.ExamId equals e.Id
            select new { r.MarksObtained, e.TotalMarks }
        ).ToListAsync(cancellationToken);

        var totalResults = resultProjections.Count;
        var passCount = resultProjections.Count(x => x.MarksObtained >= x.TotalMarks * 0.5m);
        var examPassRate = totalResults == 0
            ? 0.0
            : Math.Round((double)passCount / totalResults * 100, 1);
        var examAverageScore = totalResults == 0
            ? 0.0
            : Math.Round((double)resultProjections.Average(x => x.MarksObtained), 1);

        return new KpiResponse(
            totalStudents,
            totalTeachers,
            totalClasses,
            attendanceRate,
            presentCount,
            absentCount,
            lateCount,
            excusedCount,
            unpaidFeesCount,
            totalCollectedPayments,
            recentNotificationsCount,
            examPassRate,
            examAverageScore);
    }

    // -----------------------------------------------------------------------
    // Attendance trend (per-day breakdown for last N days)
    // -----------------------------------------------------------------------

    public async Task<AttendanceTrendsResponse> GetAttendanceTrendsAsync(int days, CancellationToken cancellationToken)
    {
        days = Math.Clamp(days, 7, 90);
        var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));

        _logger.LogInformation("Fetching attendance trends for last {Days} days.", days);

        var rawTrends = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(x => x.Date >= fromDate)
            .GroupBy(x => x.Date)
            .Select(g => new
            {
                Date = g.Key,
                Present = g.Count(x => x.Status == AttendanceStatus.Present),
                Absent  = g.Count(x => x.Status == AttendanceStatus.Absent),
                Late    = g.Count(x => x.Status == AttendanceStatus.Late),
                Excused = g.Count(x => x.Status == AttendanceStatus.Excused)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        var trends = rawTrends
            .Select(x => new AttendanceTrendPoint(
                x.Date.ToString("yyyy-MM-dd"),
                x.Present,
                x.Absent,
                x.Late,
                x.Excused))
            .ToArray();

        return new AttendanceTrendsResponse(trends, days);
    }

    // -----------------------------------------------------------------------
    // Exam performance (per-exam average + pass/fail, optional class filter)
    // -----------------------------------------------------------------------

    public async Task<ExamPerformanceResponse> GetExamPerformanceAsync(Guid? classId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching exam performance. ClassId filter: {ClassId}", classId);

        var examQuery = _context.Exams.AsNoTracking();

        if (classId.HasValue)
        {
            examQuery = examQuery.Where(e => e.ClassId == classId.Value);
        }

        // Project each exam with its aggregated result stats
        var examStats = await examQuery
            .Select(e => new
            {
                ExamTitle      = e.Title,
                SubjectName    = e.Subject != null ? e.Subject.Name : string.Empty,
                ClassName      = e.Class != null ? e.Class.Name + " " + e.Class.Section : string.Empty,
                TotalMarks     = e.TotalMarks,
                ResultCount    = e.Results.Count(),
                PassCount      = e.Results.Count(r => r.MarksObtained >= e.TotalMarks * 0.5m),
                TotalScore     = e.Results.Sum(r => (decimal?)r.MarksObtained) ?? 0m
            })
            .OrderBy(e => e.ExamTitle)
            .ToListAsync(cancellationToken);

        // Filter to exams with at least one result (others have no data to chart)
        var withResults = examStats.Where(e => e.ResultCount > 0).ToList();

        var items = withResults
            .Select(e => new ExamPerformanceItem(
                e.ExamTitle,
                e.SubjectName,
                e.ClassName,
                e.ResultCount == 0 ? 0.0 : Math.Round((double)e.TotalScore / e.ResultCount, 1),
                e.TotalMarks,
                e.PassCount,
                e.ResultCount - e.PassCount,
                e.ResultCount))
            .ToArray();

        var overallTotalSubmissions = items.Sum(x => x.TotalSubmissions);
        var overallPassCount = items.Sum(x => x.PassCount);
        var overallPassRate = overallTotalSubmissions == 0
            ? 0.0
            : Math.Round((double)overallPassCount / overallTotalSubmissions * 100, 1);
        var overallAvg = items.Length == 0
            ? 0.0
            : Math.Round(items.Average(x => x.AverageScore), 1);

        return new ExamPerformanceResponse(items, overallPassRate, overallAvg, items.Length);
    }

    // -----------------------------------------------------------------------
    // Finance summary (fee status breakdown + collected payments)
    // -----------------------------------------------------------------------

    public async Task<FinanceSummaryResponse> GetFinanceSummaryAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching finance summary.");

        // Single GROUP BY query on Fees
        var feesByStatus = await _context.Fees
            .AsNoTracking()
            .GroupBy(f => f.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count  = g.Count(),
                Amount = g.Sum(f => f.Amount)
            })
            .ToListAsync(cancellationToken);

        decimal GetAmount(FeeStatus status) =>
            feesByStatus.FirstOrDefault(x => x.Status == status)?.Amount ?? 0m;
        int GetCount(FeeStatus status) =>
            feesByStatus.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

        // Total collected from the Payments table (actual money received)
        var totalCollectedPayments = await _context.Payments
            .AsNoTracking()
            .SumAsync(p => (decimal?)p.AmountPaid, cancellationToken) ?? 0m;

        var totalPaymentsCount = await _context.Payments
            .CountAsync(cancellationToken);

        var paidAmount         = GetAmount(FeeStatus.Paid);
        var pendingAmount      = GetAmount(FeeStatus.Pending);
        var overdueAmount      = GetAmount(FeeStatus.Overdue);
        var partiallyPaidAmount = GetAmount(FeeStatus.PartiallyPaid);
        var totalFeesAmount    = paidAmount + pendingAmount + overdueAmount + partiallyPaidAmount;

        return new FinanceSummaryResponse(
            totalFeesAmount,
            paidAmount,
            pendingAmount,
            overdueAmount,
            partiallyPaidAmount,
            GetCount(FeeStatus.Paid),
            GetCount(FeeStatus.Pending),
            GetCount(FeeStatus.Overdue),
            GetCount(FeeStatus.PartiallyPaid),
            totalCollectedPayments,
            totalPaymentsCount);
    }
}
