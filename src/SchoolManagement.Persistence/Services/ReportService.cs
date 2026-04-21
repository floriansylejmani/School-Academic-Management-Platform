using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Reports;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Persistence.Services;

public sealed class ReportService : IReportService
{
    private sealed record AttendanceAggregate(Guid StudentId, AttendanceStatus Status);
    private sealed record ResultAggregate(Guid StudentId, decimal MarksObtained, decimal TotalMarks);
    private sealed record FeeAggregate(Guid StudentId, decimal Amount, decimal PaidAmount);

    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StudentsPdfReportData> GetStudentsReportAsync(ReportPdfFilterRequest request, CancellationToken cancellationToken)
    {
        var students = await BuildStudentQuery(request)
            .Select(student => new
            {
                student.Id,
                StudentName = student.User!.FullName,
                student.StudentCode,
                student.User!.Email,
                ClassName = student.Class != null ? $"{student.Class.Name} {student.Class.Section}" : "Unassigned",
                ParentName = student.Parent != null ? student.Parent.User!.FullName : null,
                student.AdmissionDate
            })
            .OrderBy(student => student.StudentName)
            .ToListAsync(cancellationToken);

        var studentIds = students.Select(student => student.Id).ToArray();

        var attendance = studentIds.Length == 0
            ? []
            : await _context.AttendanceRecords.AsNoTracking()
                .Where(record => studentIds.Contains(record.StudentId))
                .Where(record => !request.DateFrom.HasValue || record.Date >= request.DateFrom.Value)
                .Where(record => !request.DateTo.HasValue || record.Date <= request.DateTo.Value)
                .Select(record => new AttendanceAggregate(record.StudentId, record.Status))
                .ToListAsync(cancellationToken);

        var results = studentIds.Length == 0
            ? []
            : await _context.Results.AsNoTracking()
                .Include(result => result.Exam)
                .Where(result => studentIds.Contains(result.StudentId))
                .Where(result => !request.DateFrom.HasValue || result.Exam!.ExamDate >= request.DateFrom.Value)
                .Where(result => !request.DateTo.HasValue || result.Exam!.ExamDate <= request.DateTo.Value)
                .Select(result => new ResultAggregate(result.StudentId, result.MarksObtained, result.Exam!.TotalMarks))
                .ToListAsync(cancellationToken);

        var fees = studentIds.Length == 0
            ? []
            : await _context.Fees.AsNoTracking()
                .Include(fee => fee.Payments)
                .Where(fee => studentIds.Contains(fee.StudentId))
                .Where(fee => !request.DateFrom.HasValue || fee.DueDate >= request.DateFrom.Value)
                .Where(fee => !request.DateTo.HasValue || fee.DueDate <= request.DateTo.Value)
                .Select(fee => new FeeAggregate(fee.StudentId, fee.Amount, fee.Payments.Sum(payment => payment.AmountPaid)))
                .ToListAsync(cancellationToken);

        var attendanceLookup = attendance.GroupBy(item => item.StudentId).ToDictionary(group => group.Key, group => group.ToArray());
        var resultsLookup = results.GroupBy(item => item.StudentId).ToDictionary(group => group.Key, group => group.ToArray());
        var feesLookup = fees.GroupBy(item => item.StudentId).ToDictionary(group => group.Key, group => group.ToArray());

        var rows = students
            .Select(student =>
            {
                attendanceLookup.TryGetValue(student.Id, out var studentAttendance);
                resultsLookup.TryGetValue(student.Id, out var studentResults);
                feesLookup.TryGetValue(student.Id, out var studentFees);

                var attendancePercentage = CalculateAttendancePercentage(studentAttendance);
                var averageScore = CalculateAverageScore(studentResults);
                var totalBilled = studentFees?.Sum(item => item.Amount) ?? 0m;
                var totalPaid = studentFees?.Sum(item => item.PaidAmount) ?? 0m;

                return new StudentReportRow(
                    student.StudentName,
                    student.StudentCode,
                    student.Email,
                    student.ClassName,
                    student.ParentName,
                    student.AdmissionDate,
                    attendancePercentage,
                    averageScore,
                    totalBilled,
                    totalPaid,
                    totalBilled - totalPaid,
                    studentResults?.Length ?? 0);
            })
            .ToArray();

        return new StudentsPdfReportData(
            await BuildMetadataAsync("Students Report", request, cancellationToken),
            rows);
    }

    public async Task<AttendancePdfReportData> GetAttendanceReportAsync(ReportPdfFilterRequest request, CancellationToken cancellationToken)
    {
        var records = await _context.AttendanceRecords.AsNoTracking()
            .Include(record => record.Student)!.ThenInclude(student => student!.User)
            .Include(record => record.Class)
            .Include(record => record.Subject)
            .Include(record => record.Teacher)!.ThenInclude(teacher => teacher!.User)
            .Where(record => !request.ClassId.HasValue || record.ClassId == request.ClassId.Value)
            .Where(record => !request.StudentId.HasValue || record.StudentId == request.StudentId.Value)
            .Where(record => !request.DateFrom.HasValue || record.Date >= request.DateFrom.Value)
            .Where(record => !request.DateTo.HasValue || record.Date <= request.DateTo.Value)
            .OrderByDescending(record => record.Date)
            .ThenBy(record => record.Student!.User!.FullName)
            .Select(record => new AttendanceReportRow(
                record.Student!.User!.FullName,
                record.Class != null ? $"{record.Class.Name} {record.Class.Section}" : string.Empty,
                record.Subject!.Name,
                record.Teacher!.User!.FullName,
                record.Date,
                record.Status.ToString(),
                record.Remarks))
            .ToListAsync(cancellationToken);

        return new AttendancePdfReportData(
            await BuildMetadataAsync("Attendance Report", request, cancellationToken),
            records.Count(record => record.Status == AttendanceStatus.Present.ToString()),
            records.Count(record => record.Status == AttendanceStatus.Absent.ToString()),
            records.Count(record => record.Status == AttendanceStatus.Late.ToString()),
            records.Count(record => record.Status == AttendanceStatus.Excused.ToString()),
            records);
    }

    public async Task<FeesPdfReportData> GetFeesReportAsync(ReportPdfFilterRequest request, CancellationToken cancellationToken)
    {
        var fees = await _context.Fees.AsNoTracking()
            .Include(fee => fee.Student)!.ThenInclude(student => student!.User)
            .Include(fee => fee.Student)!.ThenInclude(student => student!.Class)
            .Include(fee => fee.Payments)
            .Where(fee => !request.ClassId.HasValue || fee.Student!.ClassId == request.ClassId.Value)
            .Where(fee => !request.StudentId.HasValue || fee.StudentId == request.StudentId.Value)
            .Where(fee => !request.DateFrom.HasValue || fee.DueDate >= request.DateFrom.Value)
            .Where(fee => !request.DateTo.HasValue || fee.DueDate <= request.DateTo.Value)
            .OrderByDescending(fee => fee.DueDate)
            .ThenBy(fee => fee.Student!.User!.FullName)
            .Select(fee => new FeeReportRow(
                fee.Student!.User!.FullName,
                fee.Student.Class != null ? $"{fee.Student.Class.Name} {fee.Student.Class.Section}" : "Unassigned",
                fee.FeeType,
                fee.Amount,
                fee.Payments.Sum(payment => payment.AmountPaid),
                fee.Amount - fee.Payments.Sum(payment => payment.AmountPaid),
                fee.DueDate,
                fee.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new FeesPdfReportData(
            await BuildMetadataAsync("Fees Report", request, cancellationToken),
            fees.Sum(fee => fee.Amount),
            fees.Sum(fee => fee.PaidAmount),
            fees.Sum(fee => fee.OutstandingBalance),
            fees);
    }

    private IQueryable<Student> BuildStudentQuery(ReportPdfFilterRequest request)
    {
        return _context.Students.AsNoTracking()
            .Include(student => student.User)
            .Include(student => student.Class)
            .Include(student => student.Parent)!.ThenInclude(parent => parent!.User)
            .Where(student => !request.ClassId.HasValue || student.ClassId == request.ClassId.Value)
            .Where(student => !request.StudentId.HasValue || student.Id == request.StudentId.Value);
    }

    private async Task<ReportMetadata> BuildMetadataAsync(string title, ReportPdfFilterRequest request, CancellationToken cancellationToken)
    {
        var className = request.ClassId.HasValue
            ? await _context.AcademicClasses.AsNoTracking()
                .Where(academicClass => academicClass.Id == request.ClassId.Value)
                .Select(academicClass => $"{academicClass.Name} {academicClass.Section}")
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        var studentName = request.StudentId.HasValue
            ? await _context.Students.AsNoTracking()
                .Include(student => student.User)
                .Where(student => student.Id == request.StudentId.Value)
                .Select(student => student.User!.FullName)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        return new ReportMetadata(
            title,
            className,
            studentName,
            request.DateFrom,
            request.DateTo,
            DateTime.UtcNow);
    }

    private static decimal CalculateAttendancePercentage(IEnumerable<AttendanceAggregate>? attendance)
    {
        var records = attendance?.ToArray() ?? [];
        if (records.Length == 0)
        {
            return 0m;
        }

        var presentCount = records.Count(record => record.Status == AttendanceStatus.Present);
        return Math.Round((decimal)presentCount * 100m / records.Length, 1);
    }

    private static decimal CalculateAverageScore(IEnumerable<ResultAggregate>? results)
    {
        var scoredResults = results?.Where(result => result.TotalMarks > 0).ToArray() ?? [];
        if (scoredResults.Length == 0)
        {
            return 0m;
        }

        return Math.Round(scoredResults.Average(result => result.MarksObtained / result.TotalMarks * 100m), 1);
    }
}
