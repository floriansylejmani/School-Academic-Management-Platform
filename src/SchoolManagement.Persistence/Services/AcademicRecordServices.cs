using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Exams;
using SchoolManagement.Application.Notifications;
using SchoolManagement.Application.Results;
using SchoolManagement.Application.Timetable;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Persistence.Common;

namespace SchoolManagement.Persistence.Services;

public sealed class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _context;
    private readonly TeacherScopeService _teacherScopeService;
    private readonly IAttendanceRealtimeNotifier _attendanceRealtimeNotifier;
    private readonly NotificationRealtimeDispatcher _notificationRealtimeDispatcher;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        AppDbContext context,
        TeacherScopeService teacherScopeService,
        IAttendanceRealtimeNotifier attendanceRealtimeNotifier,
        NotificationRealtimeDispatcher notificationRealtimeDispatcher,
        ILogger<AttendanceService> logger)
    {
        _context = context;
        _teacherScopeService = teacherScopeService;
        _attendanceRealtimeNotifier = attendanceRealtimeNotifier;
        _notificationRealtimeDispatcher = notificationRealtimeDispatcher;
        _logger = logger;
    }

    public async Task<PagedResponse<AttendanceResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        return await BuildAttendanceQuery().OrderByDescending(x => x.Date).ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<AttendanceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var attendance = await BuildAttendanceQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Attendance record not found.", 404);

        return attendance.ToResponse();
    }

    public async Task<IReadOnlyCollection<AttendanceResponse>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var records = await BuildAttendanceQuery()
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);

        return records.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<PagedResponse<AttendanceResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);

        return await _teacherScopeService.ApplyAttendanceScope(BuildAttendanceQuery(), teacherId)
            .OrderByDescending(x => x.Date)
            .ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<IReadOnlyCollection<AttendanceResponse>> GetForTeacherUserByStudentIdAsync(Guid teacherUserId, Guid studentId, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanAccessStudentAsync(teacherId, studentId, cancellationToken);

        var records = await _teacherScopeService.ApplyAttendanceScope(BuildAttendanceQuery(), teacherId)
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);

        return records.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<AttendanceResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        var attendance = await _teacherScopeService.ApplyAttendanceScope(BuildAttendanceQuery(), teacherId)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Attendance record not found.", 404);

        return attendance.ToResponse();
    }

    public async Task<AttendanceResponse> CreateForTeacherUserAsync(Guid teacherUserId, CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanManageAttendanceAsync(teacherId, request.StudentId, request.ClassId, request.SubjectId, cancellationToken);
        return await CreateAsync(request with { TeacherId = teacherId }, cancellationToken);
    }

    public async Task<AttendanceResponse> UpdateForTeacherUserAsync(Guid teacherUserId, Guid id, UpdateAttendanceRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanManageAttendanceRecordAsync(teacherId, id, cancellationToken);
        await _teacherScopeService.EnsureCanManageAttendanceAsync(teacherId, request.StudentId, request.ClassId, request.SubjectId, cancellationToken);
        return await UpdateAsync(id, request with { TeacherId = teacherId }, cancellationToken);
    }

    public async Task<AttendanceResponse> CreateAsync(CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        await EnsureAttendanceReferencesExistAsync(request.StudentId, request.ClassId, request.SubjectId, request.TeacherId, cancellationToken);

        var attendance = new AttendanceRecord
        {
            StudentId = request.StudentId,
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            TeacherId = request.TeacherId,
            Date = request.Date,
            Status = ParseAttendanceStatus(request.Status),
            Remarks = request.Remarks?.Trim()
        };

        _context.AttendanceRecords.Add(attendance);

        var attendanceNotifications = await BuildAttendanceNotificationEntitiesAsync(
            request.StudentId,
            request.SubjectId,
            request.Date,
            request.Status,
            isUpdate: false,
            cancellationToken);

        if (attendanceNotifications.Count > 0)
        {
            _context.Notifications.AddRange(attendanceNotifications);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var response = await GetByIdAsync(attendance.Id, cancellationToken);
        var audienceUserIds = await ResolveAttendanceAudienceUserIdsAsync(response.StudentId, response.TeacherId, cancellationToken);

        await PublishAttendanceRealtimeAsync(audienceUserIds, AttendanceRealtimeEventTypes.Created, response, cancellationToken);
        await _notificationRealtimeDispatcher.BroadcastCreatedAsync(attendanceNotifications.Select(x => x.Id), cancellationToken);

        return response;
    }

    public async Task<AttendanceResponse> UpdateAsync(Guid id, UpdateAttendanceRequest request, CancellationToken cancellationToken)
    {
        var attendance = await _context.AttendanceRecords.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Attendance record not found.", 404);

        await EnsureAttendanceReferencesExistAsync(request.StudentId, request.ClassId, request.SubjectId, request.TeacherId, cancellationToken);
        var previousAudienceUserIds = await ResolveAttendanceAudienceUserIdsAsync(attendance.StudentId, attendance.TeacherId, cancellationToken);
        var parsedStatus = ParseAttendanceStatus(request.Status);
        var trimmedRemarks = request.Remarks?.Trim();
        var hasMeaningfulChange =
            attendance.StudentId != request.StudentId ||
            attendance.ClassId != request.ClassId ||
            attendance.SubjectId != request.SubjectId ||
            attendance.TeacherId != request.TeacherId ||
            attendance.Date != request.Date ||
            attendance.Status != parsedStatus ||
            attendance.Remarks != trimmedRemarks;

        if (!hasMeaningfulChange)
        {
            return await GetByIdAsync(id, cancellationToken);
        }

        var attendanceNotifications = await BuildAttendanceNotificationEntitiesAsync(
            request.StudentId,
            request.SubjectId,
            request.Date,
            request.Status,
            isUpdate: true,
            cancellationToken);

        attendance.StudentId = request.StudentId;
        attendance.ClassId = request.ClassId;
        attendance.SubjectId = request.SubjectId;
        attendance.TeacherId = request.TeacherId;
        attendance.Date = request.Date;
        attendance.Status = parsedStatus;
        attendance.Remarks = trimmedRemarks;

        if (attendanceNotifications.Count > 0)
        {
            _context.Notifications.AddRange(attendanceNotifications);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var response = await GetByIdAsync(id, cancellationToken);
        var currentAudienceUserIds = await ResolveAttendanceAudienceUserIdsAsync(response.StudentId, response.TeacherId, cancellationToken);

        await PublishAttendanceRealtimeAsync(
            previousAudienceUserIds.Concat(currentAudienceUserIds),
            AttendanceRealtimeEventTypes.Updated,
            response,
            cancellationToken);

        await _notificationRealtimeDispatcher.BroadcastCreatedAsync(attendanceNotifications.Select(x => x.Id), cancellationToken);

        return response;
    }

    private IQueryable<AttendanceRecord> BuildAttendanceQuery()
    {
        return _context.AttendanceRecords.AsNoTracking()
            .Include(x => x.Student).ThenInclude(x => x!.User)
            .Include(x => x.Class)
            .Include(x => x.Subject)
            .Include(x => x.Teacher).ThenInclude(x => x!.User);
    }

    private static AttendanceStatus ParseAttendanceStatus(string status)
    {
        if (!Enum.TryParse<AttendanceStatus>(status, true, out var parsed))
        {
            throw new AppException("Attendance status is invalid.");
        }

        return parsed;
    }

    private async Task EnsureAttendanceReferencesExistAsync(Guid studentId, Guid classId, Guid subjectId, Guid teacherId, CancellationToken cancellationToken)
    {
        if (!await _context.Students.AnyAsync(x => x.Id == studentId, cancellationToken))
        {
            throw new AppException("Student not found.", 404);
        }

        if (!await _context.AcademicClasses.AnyAsync(x => x.Id == classId, cancellationToken))
        {
            throw new AppException("Class not found.", 404);
        }

        if (!await _context.Subjects.AnyAsync(x => x.Id == subjectId, cancellationToken))
        {
            throw new AppException("Subject not found.", 404);
        }

        if (!await _context.Teachers.AnyAsync(x => x.Id == teacherId, cancellationToken))
        {
            throw new AppException("Teacher not found.", 404);
        }
    }

    private async Task<List<Notification>> BuildAttendanceNotificationEntitiesAsync(
        Guid studentId,
        Guid subjectId,
        DateOnly date,
        string status,
        bool isUpdate,
        CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .Include(x => x.User)
            .Include(x => x.Parent).ThenInclude(x => x!.User)
            .SingleOrDefaultAsync(x => x.Id == studentId, cancellationToken);

        if (student is null)
        {
            return [];
        }

        var subjectName = await _context.Subjects
            .Where(x => x.Id == subjectId)
            .Select(x => x.Name)
            .SingleOrDefaultAsync(cancellationToken) ?? "a subject";

        var dateStr = date.ToString("MMM d");
        var notifications = new List<Notification>
        {
            new()
            {
                UserId = student.UserId,
                Title = isUpdate ? "Attendance updated" : "Attendance recorded",
                Message = isUpdate
                    ? $"Your attendance for {subjectName} on {dateStr} was updated to {status}."
                    : $"Your attendance for {subjectName} on {dateStr} was marked as {status}."
            }
        };

        if (student.Parent?.UserId is { } parentUserId)
        {
            var studentName = student.User?.FullName ?? "your child";
            notifications.Add(new Notification
            {
                UserId = parentUserId,
                StudentId = student.Id,
                Title = isUpdate ? "Attendance updated" : "Attendance recorded",
                Message = isUpdate
                    ? $"{studentName}'s attendance for {subjectName} on {dateStr} was updated to {status}."
                    : $"{studentName}'s attendance for {subjectName} on {dateStr} was marked as {status}."
            });
        }

        return notifications;
    }

    private async Task<HashSet<Guid>> ResolveAttendanceAudienceUserIdsAsync(Guid studentId, Guid teacherId, CancellationToken cancellationToken)
    {
        var adminUserIds = await _context.Users
            .AsNoTracking()
            .Where(x => x.IsActive && x.Role!.Name == "Admin")
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var teacherUserId = await _context.Teachers
            .AsNoTracking()
            .Where(x => x.Id == teacherId)
            .Select(x => (Guid?)x.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        var studentAudience = await _context.Students
            .AsNoTracking()
            .Where(x => x.Id == studentId)
            .Select(x => new
            {
                x.UserId,
                ParentUserId = x.Parent != null ? x.Parent.UserId : (Guid?)null
            })
            .SingleOrDefaultAsync(cancellationToken);

        var audience = new HashSet<Guid>(adminUserIds);

        if (teacherUserId.HasValue)
        {
            audience.Add(teacherUserId.Value);
        }

        if (studentAudience is not null)
        {
            audience.Add(studentAudience.UserId);

            if (studentAudience.ParentUserId.HasValue)
            {
                audience.Add(studentAudience.ParentUserId.Value);
            }
        }

        return audience;
    }

    private async Task PublishAttendanceRealtimeAsync(
        IEnumerable<Guid> audienceUserIds,
        string eventType,
        AttendanceResponse response,
        CancellationToken cancellationToken)
    {
        try
        {
            await _attendanceRealtimeNotifier.BroadcastAttendanceChangedAsync(
                audienceUserIds,
                new AttendanceRealtimeEvent(eventType, response, DateTime.UtcNow),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to broadcast attendance realtime event. AttendanceId: {AttendanceId}, EventType: {EventType}",
                response.Id,
                eventType);
        }
    }
}

public sealed class ExamService : IExamService
{
    private readonly AppDbContext _context;
    private readonly TeacherScopeService _teacherScopeService;
    private readonly NotificationRealtimeDispatcher _notificationRealtimeDispatcher;

    public ExamService(AppDbContext context, TeacherScopeService teacherScopeService, NotificationRealtimeDispatcher notificationRealtimeDispatcher)
    {
        _context = context;
        _teacherScopeService = teacherScopeService;
        _notificationRealtimeDispatcher = notificationRealtimeDispatcher;
    }

    public async Task<PagedResponse<ExamResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        return await BuildExamQuery().OrderByDescending(x => x.ExamDate).ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<ExamResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var exam = await BuildExamQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Exam not found.", 404);

        return exam.ToResponse();
    }

    public async Task<IReadOnlyCollection<ExamResponse>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken)
    {
        var exams = await BuildExamQuery()
            .Where(x => x.ClassId == classId)
            .OrderByDescending(x => x.ExamDate)
            .ToListAsync(cancellationToken);

        return exams.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<PagedResponse<ExamResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);

        return await _teacherScopeService.ApplyExamScope(BuildExamQuery(), teacherId)
            .OrderByDescending(x => x.ExamDate)
            .ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<IReadOnlyCollection<ExamResponse>> GetForTeacherUserByClassIdAsync(Guid teacherUserId, Guid classId, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanAccessClassAsync(teacherId, classId, cancellationToken);

        var exams = await _teacherScopeService.ApplyExamScope(BuildExamQuery(), teacherId)
            .Where(x => x.ClassId == classId)
            .OrderByDescending(x => x.ExamDate)
            .ToListAsync(cancellationToken);

        return exams.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<ExamResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        var exam = await _teacherScopeService.ApplyExamScope(BuildExamQuery(), teacherId)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Exam not found.", 404);

        return exam.ToResponse();
    }

    public async Task<ExamResponse> CreateForTeacherUserAsync(Guid teacherUserId, CreateExamRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanManageClassSubjectAsync(teacherId, request.ClassId, request.SubjectId, cancellationToken);
        return await CreateAsync(request, cancellationToken);
    }

    public async Task<ExamResponse> UpdateForTeacherUserAsync(Guid teacherUserId, Guid id, UpdateExamRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanManageExamAsync(teacherId, id, cancellationToken);
        await _teacherScopeService.EnsureCanManageClassSubjectAsync(teacherId, request.ClassId, request.SubjectId, cancellationToken);
        return await UpdateAsync(id, request, cancellationToken);
    }

    public async Task DeleteForTeacherUserAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanManageExamAsync(teacherId, id, cancellationToken);
        await DeleteAsync(id, cancellationToken);
    }

    public async Task<ExamResponse> CreateAsync(CreateExamRequest request, CancellationToken cancellationToken)
    {
        await EnsureExamReferencesExistAsync(request.ClassId, request.SubjectId, cancellationToken);
        var title = request.Title.Trim();

        var exam = new Exam
        {
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            Title = title,
            ExamDate = request.ExamDate,
            TotalMarks = request.TotalMarks
        };

        _context.Exams.Add(exam);

        var notificationIds = await AddExamNotificationsAsync(request.ClassId, request.SubjectId, title, request.ExamDate, isUpdate: false, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await _notificationRealtimeDispatcher.BroadcastCreatedAsync(notificationIds, cancellationToken);
        return await GetByIdAsync(exam.Id, cancellationToken);
    }

    public async Task<ExamResponse> UpdateAsync(Guid id, UpdateExamRequest request, CancellationToken cancellationToken)
    {
        var exam = await _context.Exams.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Exam not found.", 404);

        await EnsureExamReferencesExistAsync(request.ClassId, request.SubjectId, cancellationToken);

        var title = request.Title.Trim();
        var hasNotificationRelevantChanges =
            exam.ClassId != request.ClassId ||
            exam.SubjectId != request.SubjectId ||
            exam.Title != title ||
            exam.ExamDate != request.ExamDate;

        exam.ClassId = request.ClassId;
        exam.SubjectId = request.SubjectId;
        exam.Title = title;
        exam.ExamDate = request.ExamDate;
        exam.TotalMarks = request.TotalMarks;

        IReadOnlyCollection<Guid> notificationIds = [];
        if (hasNotificationRelevantChanges)
        {
            notificationIds = await AddExamNotificationsAsync(request.ClassId, request.SubjectId, title, request.ExamDate, isUpdate: true, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _notificationRealtimeDispatcher.BroadcastCreatedAsync(notificationIds, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var exam = await _context.Exams.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Exam not found.", 404);

        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<Guid>> AddExamNotificationsAsync(Guid classId, Guid subjectId, string title, DateOnly examDate, bool isUpdate, CancellationToken cancellationToken)
    {
        var students = await _context.Students
            .Include(x => x.User)
            .Include(x => x.Parent).ThenInclude(x => x!.User)
            .Where(x => x.ClassId == classId)
            .ToListAsync(cancellationToken);

        var studentUserIds = students
            .Select(x => x.UserId)
            .Distinct()
            .ToList();

        var teacherUserIds = await _context.TimetableEntries
            .Where(x => x.ClassId == classId && x.SubjectId == subjectId)
            .Select(x => x.Teacher!.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (studentUserIds.Count == 0 && !students.Any(x => x.Parent?.UserId is not null) && teacherUserIds.Count == 0)
        {
            return [];
        }

        var verb = isUpdate ? "rescheduled" : "scheduled";
        var dateStr = examDate.ToString("MMM d, yyyy");
        var notifications = new List<Notification>();

        notifications.AddRange(studentUserIds.Select(uid => new Notification
        {
            UserId = uid,
            Title = isUpdate ? "Exam updated" : "New exam scheduled",
            Message = $"The exam '{title}' has been {verb} for {dateStr}."
        }));

        notifications.AddRange(
            students
                .Where(x => x.Parent?.UserId is { })
                .Select(x => new Notification
                {
                    UserId = x.Parent!.UserId,
                    StudentId = x.Id,
                    Title = isUpdate ? "Exam updated" : "New exam scheduled",
                    Message = $"{x.User!.FullName}'s exam '{title}' has been {verb} for {dateStr}."
                }));

        notifications.AddRange(teacherUserIds.Select(uid => new Notification
        {
            UserId = uid,
            Title = isUpdate ? "Exam updated" : "New exam scheduled",
            Message = $"The exam '{title}' has been {verb} for one of your assigned classes on {dateStr}."
        }));

        _context.Notifications.AddRange(notifications);
        return notifications.Select(x => x.Id).ToArray();
    }

    private IQueryable<Exam> BuildExamQuery()
    {
        return _context.Exams.AsNoTracking().Include(x => x.Class).Include(x => x.Subject);
    }

    private async Task EnsureExamReferencesExistAsync(Guid classId, Guid subjectId, CancellationToken cancellationToken)
    {
        if (!await _context.AcademicClasses.AnyAsync(x => x.Id == classId, cancellationToken))
        {
            throw new AppException("Class not found.", 404);
        }

        if (!await _context.Subjects.AnyAsync(x => x.Id == subjectId, cancellationToken))
        {
            throw new AppException("Subject not found.", 404);
        }
    }
}

public sealed class TimetableService : ITimetableService
{
    private readonly AppDbContext _context;
    private readonly TeacherScopeService _teacherScopeService;

    public TimetableService(AppDbContext context, TeacherScopeService teacherScopeService)
    {
        _context = context;
        _teacherScopeService = teacherScopeService;
    }

    public async Task<PagedResponse<TimetableEntryResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        return await BuildTimetableQuery().OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<IReadOnlyCollection<TimetableEntryResponse>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken)
    {
        var entries = await BuildTimetableQuery()
            .Where(x => x.ClassId == classId)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        return entries.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<TimetableEntryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await BuildTimetableQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Timetable entry not found.", 404);

        return entry.ToResponse();
    }

    public async Task<PagedResponse<TimetableEntryResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);

        return await _teacherScopeService.ApplyTimetableScope(BuildTimetableQuery(), teacherId)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<IReadOnlyCollection<TimetableEntryResponse>> GetForTeacherUserByClassIdAsync(Guid teacherUserId, Guid classId, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanAccessClassAsync(teacherId, classId, cancellationToken);

        var entries = await _teacherScopeService.ApplyTimetableScope(BuildTimetableQuery(), teacherId)
            .Where(x => x.ClassId == classId)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        return entries.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<TimetableEntryResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        var entry = await _teacherScopeService.ApplyTimetableScope(BuildTimetableQuery(), teacherId)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Timetable entry not found.", 404);

        return entry.ToResponse();
    }

    public async Task<TimetableEntryResponse> CreateAsync(CreateTimetableEntryRequest request, CancellationToken cancellationToken)
    {
        await EnsureTimetableReferencesExistAsync(request.ClassId, request.SubjectId, request.TeacherId, cancellationToken);

        var entry = new TimetableEntry
        {
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            TeacherId = request.TeacherId,
            DayOfWeek = ParseDayOfWeek(request.DayOfWeek),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RoomNumber = request.RoomNumber?.Trim()
        };

        _context.TimetableEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entry.Id, cancellationToken);
    }

    public async Task<TimetableEntryResponse> UpdateAsync(Guid id, UpdateTimetableEntryRequest request, CancellationToken cancellationToken)
    {
        var entry = await _context.TimetableEntries.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Timetable entry not found.", 404);

        await EnsureTimetableReferencesExistAsync(request.ClassId, request.SubjectId, request.TeacherId, cancellationToken);

        entry.ClassId = request.ClassId;
        entry.SubjectId = request.SubjectId;
        entry.TeacherId = request.TeacherId;
        entry.DayOfWeek = ParseDayOfWeek(request.DayOfWeek);
        entry.StartTime = request.StartTime;
        entry.EndTime = request.EndTime;
        entry.RoomNumber = request.RoomNumber?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _context.TimetableEntries.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Timetable entry not found.", 404);

        _context.TimetableEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TimetableEntry> BuildTimetableQuery()
    {
        return _context.TimetableEntries.AsNoTracking()
            .Include(x => x.Class)
            .Include(x => x.Subject)
            .Include(x => x.Teacher).ThenInclude(x => x!.User);
    }

    private async Task EnsureTimetableReferencesExistAsync(Guid classId, Guid subjectId, Guid teacherId, CancellationToken cancellationToken)
    {
        if (!await _context.AcademicClasses.AnyAsync(x => x.Id == classId, cancellationToken))
        {
            throw new AppException("Class not found.", 404);
        }

        if (!await _context.Subjects.AnyAsync(x => x.Id == subjectId, cancellationToken))
        {
            throw new AppException("Subject not found.", 404);
        }

        if (!await _context.Teachers.AnyAsync(x => x.Id == teacherId, cancellationToken))
        {
            throw new AppException("Teacher not found.", 404);
        }
    }

    private static DayOfWeek ParseDayOfWeek(string dayOfWeek)
    {
        if (!Enum.TryParse<DayOfWeek>(dayOfWeek, true, out var parsed))
        {
            throw new AppException("Day of week is invalid.");
        }

        return parsed;
    }
}

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly NotificationRealtimeDispatcher _notificationRealtimeDispatcher;

    public NotificationService(AppDbContext context, NotificationRealtimeDispatcher notificationRealtimeDispatcher)
    {
        _context = context;
        _notificationRealtimeDispatcher = notificationRealtimeDispatcher;
    }

    public async Task<PagedResponse<NotificationResponse>> GetByUserIdAsync(Guid userId, NotificationQueryRequest request, CancellationToken cancellationToken)
    {
        var query = _context.Notifications.AsNoTracking()
            .Include(x => x.Student).ThenInclude(x => x!.User)
            .Where(x => x.UserId == userId);

        if (request.UnreadOnly == true)
        {
            query = query.Where(x => !x.IsRead);
        }

        if (request.StudentId.HasValue)
        {
            query = query.Where(x => x.StudentId == request.StudentId.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToPagedResponseAsync(
                new SchoolManagement.Application.Common.Models.PaginationRequest
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                },
                cancellationToken,
                x => x.ToResponse());
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);
    }

    public async Task<NotificationResponse> MarkAsReadAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .Include(x => x.Student).ThenInclude(x => x!.User)
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new AppException("Notification not found.", 404);

        var shouldBroadcast = !notification.IsRead;
        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);

        if (shouldBroadcast)
        {
            await _notificationRealtimeDispatcher.BroadcastReadAsync(userId, notification.Id, cancellationToken);
        }

        return notification.ToResponse();
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var notifications = await _context.Notifications.Where(x => x.UserId == userId && !x.IsRead).ToListAsync(cancellationToken);
        var notificationIds = notifications.Select(x => x.Id).ToArray();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _notificationRealtimeDispatcher.BroadcastReadAllAsync(userId, notificationIds, cancellationToken);
    }

    public async Task SendToUserAsync(Guid userId, string title, string message, CancellationToken cancellationToken)
    {
        if (!await _context.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken))
        {
            throw new AppException("User not found.", 404);
        }

        var notification = new Notification { UserId = userId, Title = title.Trim(), Message = message.Trim() };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
        await _notificationRealtimeDispatcher.BroadcastCreatedAsync([notification.Id], cancellationToken);
    }

    public async Task SendToRoleAsync(string roleName, string title, string message, CancellationToken cancellationToken)
    {
        var userIds = await _context.Users
            .Where(x => x.IsActive && x.Role!.Name == roleName)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return;
        }

        var notifications = userIds.Select(uid => new Notification
        {
            UserId = uid,
            Title = title.Trim(),
            Message = message.Trim()
        }).ToArray();

        _context.Notifications.AddRange(notifications);

        await _context.SaveChangesAsync(cancellationToken);
        await _notificationRealtimeDispatcher.BroadcastCreatedAsync(notifications.Select(x => x.Id), cancellationToken);
    }
}

public sealed class ResultService : IResultService
{
    private readonly AppDbContext _context;
    private readonly TeacherScopeService _teacherScopeService;
    private readonly NotificationRealtimeDispatcher _notificationRealtimeDispatcher;

    public ResultService(AppDbContext context, TeacherScopeService teacherScopeService, NotificationRealtimeDispatcher notificationRealtimeDispatcher)
    {
        _context = context;
        _teacherScopeService = teacherScopeService;
        _notificationRealtimeDispatcher = notificationRealtimeDispatcher;
    }

    public async Task<PagedResponse<ResultResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        return await BuildResultQuery().OrderByDescending(x => x.CreatedAt).ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<IReadOnlyCollection<ResultResponse>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var results = await BuildResultQuery()
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return results.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<ResultResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await BuildResultQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Result not found.", 404);

        return result.ToResponse();
    }

    public async Task<PagedResponse<ResultResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);

        return await _teacherScopeService.ApplyResultScope(BuildResultQuery(), teacherId)
            .OrderByDescending(x => x.CreatedAt)
            .ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<IReadOnlyCollection<ResultResponse>> GetForTeacherUserByStudentIdAsync(Guid teacherUserId, Guid studentId, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanAccessStudentAsync(teacherId, studentId, cancellationToken);

        var results = await _teacherScopeService.ApplyResultScope(BuildResultQuery(), teacherId)
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return results.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<ResultResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        var result = await _teacherScopeService.ApplyResultScope(BuildResultQuery(), teacherId)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Result not found.", 404);

        return result.ToResponse();
    }

    public async Task<ResultResponse> CreateForTeacherUserAsync(Guid teacherUserId, CreateResultRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureResultMatchesTeacherScopeAsync(teacherId, request.ExamId, request.StudentId, cancellationToken);
        return await CreateAsync(request, cancellationToken);
    }

    public async Task<ResultResponse> UpdateForTeacherUserAsync(Guid teacherUserId, Guid id, UpdateResultRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanManageResultAsync(teacherId, id, cancellationToken);
        await _teacherScopeService.EnsureResultMatchesTeacherScopeAsync(teacherId, request.ExamId, request.StudentId, cancellationToken);
        return await UpdateAsync(id, request, cancellationToken);
    }

    public async Task<ResultResponse> CreateAsync(CreateResultRequest request, CancellationToken cancellationToken)
    {
        await EnsureResultReferencesAsync(request.ExamId, request.StudentId, cancellationToken);

        if (await _context.Results.AnyAsync(x => x.ExamId == request.ExamId && x.StudentId == request.StudentId, cancellationToken))
        {
            throw new AppException("Result for this student and exam already exists.");
        }

        var exam = await _context.Exams.SingleOrDefaultAsync(x => x.Id == request.ExamId, cancellationToken)
            ?? throw new AppException("Exam not found.", 404);
        if (request.MarksObtained > exam.TotalMarks)
        {
            throw new AppException("Marks obtained cannot exceed the total marks.");
        }

        var result = new Result
        {
            ExamId = request.ExamId,
            StudentId = request.StudentId,
            MarksObtained = request.MarksObtained,
            Grade = request.Grade.Trim(),
            Remarks = request.Remarks?.Trim()
        };

        _context.Results.Add(result);

        var notificationIds = await AddResultNotificationsAsync(request.ExamId, request.StudentId, exam.Title, request.Grade.Trim(), cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await _notificationRealtimeDispatcher.BroadcastCreatedAsync(notificationIds, cancellationToken);
        return await GetByIdAsync(result.Id, cancellationToken);
    }

    public async Task<ResultResponse> UpdateAsync(Guid id, UpdateResultRequest request, CancellationToken cancellationToken)
    {
        var result = await _context.Results.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Result not found.", 404);

        await EnsureResultReferencesAsync(request.ExamId, request.StudentId, cancellationToken);

        if (await _context.Results.AnyAsync(x => x.Id != id && x.ExamId == request.ExamId && x.StudentId == request.StudentId, cancellationToken))
        {
            throw new AppException("Result for this student and exam already exists.");
        }

        var exam = await _context.Exams.SingleOrDefaultAsync(x => x.Id == request.ExamId, cancellationToken)
            ?? throw new AppException("Exam not found.", 404);
        if (request.MarksObtained > exam.TotalMarks)
        {
            throw new AppException("Marks obtained cannot exceed the total marks.");
        }

        result.ExamId = request.ExamId;
        result.StudentId = request.StudentId;
        result.MarksObtained = request.MarksObtained;
        result.Grade = request.Grade.Trim();
        result.Remarks = request.Remarks?.Trim();

        var notificationIds = await AddResultNotificationsAsync(request.ExamId, request.StudentId, exam.Title, request.Grade.Trim(), cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await _notificationRealtimeDispatcher.BroadcastCreatedAsync(notificationIds, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _context.Results.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Result not found.", 404);

        _context.Results.Remove(result);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteForTeacherUserAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanManageResultAsync(teacherId, id, cancellationToken);
        await DeleteAsync(id, cancellationToken);
    }

    private async Task<IReadOnlyCollection<Guid>> AddResultNotificationsAsync(Guid examId, Guid studentId, string examTitle, string grade, CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .Include(x => x.User)
            .Include(x => x.Parent).ThenInclude(x => x!.User)
            .SingleOrDefaultAsync(x => x.Id == studentId, cancellationToken);

        if (student is null)
        {
            return [];
        }

        var notifications = new List<Notification>
        {
            new()
            {
                UserId = student.UserId,
                Title = "Result published",
                Message = $"Your result for '{examTitle}' has been published. Grade: {grade}."
            }
        };

        if (student.Parent?.UserId is { } parentUserId)
        {
            var studentName = student.User?.FullName ?? "Your child";
            notifications.Add(new Notification
            {
                UserId = parentUserId,
                StudentId = student.Id,
                Title = "Result published",
                Message = $"{studentName}'s result for '{examTitle}' has been published. Grade: {grade}."
            });
        }

        _context.Notifications.AddRange(notifications);
        return notifications.Select(x => x.Id).ToArray();
    }

    private IQueryable<Result> BuildResultQuery()
    {
        return _context.Results.AsNoTracking()
            .Include(x => x.Exam)
                .ThenInclude(x => x!.Class)
            .Include(x => x.Exam)
                .ThenInclude(x => x!.Subject)
            .Include(x => x.Student).ThenInclude(x => x!.User);
    }

    private async Task EnsureResultReferencesAsync(Guid examId, Guid studentId, CancellationToken cancellationToken)
    {
        if (!await _context.Exams.AnyAsync(x => x.Id == examId, cancellationToken))
        {
            throw new AppException("Exam not found.", 404);
        }

        if (!await _context.Students.AnyAsync(x => x.Id == studentId, cancellationToken))
        {
            throw new AppException("Student not found.", 404);
        }
    }
}
