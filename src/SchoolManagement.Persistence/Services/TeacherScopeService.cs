using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Persistence.Services;

public sealed class TeacherScopeService
{
    private readonly AppDbContext _context;

    public TeacherScopeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> GetTeacherIdByUserIdAsync(Guid teacherUserId, CancellationToken cancellationToken)
    {
        var teacherId = await _context.Teachers
            .Where(x => x.UserId == teacherUserId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return teacherId ?? throw new AppException("Teacher profile not found.", 404);
    }

    public IQueryable<AcademicClass> ApplyClassScope(IQueryable<AcademicClass> query, Guid teacherId)
    {
        return query.Where(x =>
            x.ClassTeacherId == teacherId ||
            x.SubjectAssignments.Any(a => a.TeacherId == teacherId) ||
            x.TimetableEntries.Any(t => t.TeacherId == teacherId) ||
            x.AttendanceRecords.Any(a => a.TeacherId == teacherId));
    }

    public IQueryable<Subject> ApplySubjectScope(IQueryable<Subject> query, Guid teacherId)
    {
        return query.Where(x =>
            x.TeacherAssignments.Any(a => a.TeacherId == teacherId) ||
            x.TimetableEntries.Any(t => t.TeacherId == teacherId) ||
            x.AttendanceRecords.Any(a => a.TeacherId == teacherId));
    }

    public IQueryable<Student> ApplyStudentScope(IQueryable<Student> query, Guid teacherId)
    {
        var accessibleClassIds = ApplyClassScope(_context.AcademicClasses.AsNoTracking(), teacherId)
            .Select(x => x.Id);

        return query.Where(x => x.ClassId.HasValue && accessibleClassIds.Contains(x.ClassId.Value));
    }

    public IQueryable<TimetableEntry> ApplyTimetableScope(IQueryable<TimetableEntry> query, Guid teacherId)
    {
        return query.Where(x => x.TeacherId == teacherId);
    }

    public IQueryable<AttendanceRecord> ApplyAttendanceScope(IQueryable<AttendanceRecord> query, Guid teacherId)
    {
        return query.Where(x => x.TeacherId == teacherId);
    }

    public IQueryable<Exam> ApplyExamScope(IQueryable<Exam> query, Guid teacherId)
    {
        return query.Where(x =>
            _context.TeacherSubjectAssignments.Any(a => a.TeacherId == teacherId && a.ClassId == x.ClassId && a.SubjectId == x.SubjectId) ||
            _context.TimetableEntries.Any(t => t.TeacherId == teacherId && t.ClassId == x.ClassId && t.SubjectId == x.SubjectId) ||
            _context.AttendanceRecords.Any(a => a.TeacherId == teacherId && a.ClassId == x.ClassId && a.SubjectId == x.SubjectId));
    }

    public IQueryable<Result> ApplyResultScope(IQueryable<Result> query, Guid teacherId)
    {
        return query.Where(x =>
            _context.TeacherSubjectAssignments.Any(a => a.TeacherId == teacherId && a.ClassId == x.Exam!.ClassId && a.SubjectId == x.Exam.SubjectId) ||
            _context.TimetableEntries.Any(t => t.TeacherId == teacherId && t.ClassId == x.Exam!.ClassId && t.SubjectId == x.Exam.SubjectId) ||
            _context.AttendanceRecords.Any(a => a.TeacherId == teacherId && a.ClassId == x.Exam!.ClassId && a.SubjectId == x.Exam.SubjectId));
    }

    public IQueryable<Submission> ApplySubmissionScope(IQueryable<Submission> query, Guid teacherId)
    {
        return query.Where(x =>
            _context.TeacherSubjectAssignments.Any(a => a.TeacherId == teacherId && a.ClassId == x.Exam!.ClassId && a.SubjectId == x.Exam.SubjectId) ||
            _context.TimetableEntries.Any(t => t.TeacherId == teacherId && t.ClassId == x.Exam!.ClassId && t.SubjectId == x.Exam.SubjectId) ||
            _context.AttendanceRecords.Any(a => a.TeacherId == teacherId && a.ClassId == x.Exam!.ClassId && a.SubjectId == x.Exam.SubjectId));
    }

    public async Task EnsureCanAccessClassAsync(Guid teacherId, Guid classId, CancellationToken cancellationToken)
    {
        var canAccess = await ApplyClassScope(_context.AcademicClasses.AsNoTracking(), teacherId)
            .AnyAsync(x => x.Id == classId, cancellationToken);

        if (!canAccess)
        {
            throw new AppException("You do not have permission to access this class.", 403);
        }
    }

    public async Task EnsureCanAccessStudentAsync(Guid teacherId, Guid studentId, CancellationToken cancellationToken)
    {
        var canAccess = await ApplyStudentScope(_context.Students.AsNoTracking(), teacherId)
            .AnyAsync(x => x.Id == studentId, cancellationToken);

        if (!canAccess)
        {
            throw new AppException("You do not have permission to access this student.", 403);
        }
    }

    public async Task EnsureCanManageClassSubjectAsync(Guid teacherId, Guid classId, Guid subjectId, CancellationToken cancellationToken)
    {
        var hasAssignment = await _context.TeacherSubjectAssignments
            .AnyAsync(x => x.TeacherId == teacherId && x.ClassId == classId && x.SubjectId == subjectId, cancellationToken);

        var hasTimetableEntry = await _context.TimetableEntries
            .AnyAsync(x => x.TeacherId == teacherId && x.ClassId == classId && x.SubjectId == subjectId, cancellationToken);

        var hasAttendanceHistory = await _context.AttendanceRecords
            .AnyAsync(x => x.TeacherId == teacherId && x.ClassId == classId && x.SubjectId == subjectId, cancellationToken);

        if (!hasAssignment && !hasTimetableEntry && !hasAttendanceHistory)
        {
            throw new AppException("You do not have permission to manage this class and subject combination.", 403);
        }
    }

    public async Task EnsureCanManageAttendanceAsync(Guid teacherId, Guid studentId, Guid classId, Guid subjectId, CancellationToken cancellationToken)
    {
        await EnsureCanManageClassSubjectAsync(teacherId, classId, subjectId, cancellationToken);

        var studentBelongsToClass = await _context.Students
            .AnyAsync(x => x.Id == studentId && x.ClassId == classId, cancellationToken);

        if (!studentBelongsToClass)
        {
            throw new AppException("Student does not belong to the selected class.");
        }
    }

    public async Task EnsureCanManageAttendanceRecordAsync(Guid teacherId, Guid attendanceId, CancellationToken cancellationToken)
    {
        var canManage = await ApplyAttendanceScope(_context.AttendanceRecords.AsNoTracking(), teacherId)
            .AnyAsync(x => x.Id == attendanceId, cancellationToken);

        if (!canManage)
        {
            throw new AppException("You do not have permission to manage this attendance record.", 403);
        }
    }

    public async Task EnsureCanManageExamAsync(Guid teacherId, Guid examId, CancellationToken cancellationToken)
    {
        var canManage = await ApplyExamScope(_context.Exams.AsNoTracking(), teacherId)
            .AnyAsync(x => x.Id == examId, cancellationToken);

        if (!canManage)
        {
            throw new AppException("You do not have permission to manage this exam.", 403);
        }
    }

    public async Task EnsureCanManageResultAsync(Guid teacherId, Guid resultId, CancellationToken cancellationToken)
    {
        var canManage = await ApplyResultScope(_context.Results.AsNoTracking().Include(x => x.Exam), teacherId)
            .AnyAsync(x => x.Id == resultId, cancellationToken);

        if (!canManage)
        {
            throw new AppException("You do not have permission to manage this result.", 403);
        }
    }

    public async Task EnsureResultMatchesTeacherScopeAsync(Guid teacherId, Guid examId, Guid studentId, CancellationToken cancellationToken)
    {
        var exam = await _context.Exams
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == examId, cancellationToken)
            ?? throw new AppException("Exam not found.", 404);

        await EnsureCanManageClassSubjectAsync(teacherId, exam.ClassId, exam.SubjectId, cancellationToken);

        var studentBelongsToExamClass = await _context.Students
            .AnyAsync(x => x.Id == studentId && x.ClassId == exam.ClassId, cancellationToken);

        if (!studentBelongsToExamClass)
        {
            throw new AppException("Student does not belong to the selected exam class.");
        }
    }

    public async Task EnsureCanManageSubmissionAsync(Guid teacherId, Guid submissionId, CancellationToken cancellationToken)
    {
        var canManage = await ApplySubmissionScope(_context.Submissions.AsNoTracking().Include(x => x.Exam), teacherId)
            .AnyAsync(x => x.Id == submissionId, cancellationToken);

        if (!canManage)
        {
            throw new AppException("You do not have permission to manage this submission.", 403);
        }
    }
}
