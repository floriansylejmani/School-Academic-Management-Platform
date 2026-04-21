using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.AI;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Submissions;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Persistence.Common;

namespace SchoolManagement.Persistence.Services;

public sealed class SubmissionService : ISubmissionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _context;
    private readonly TeacherScopeService _teacherScopeService;
    private readonly IAIGradingService _aiGradingService;
    private readonly NotificationRealtimeDispatcher _notificationRealtimeDispatcher;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        AppDbContext context,
        TeacherScopeService teacherScopeService,
        IAIGradingService aiGradingService,
        NotificationRealtimeDispatcher notificationRealtimeDispatcher,
        ILogger<SubmissionService> logger)
    {
        _context = context;
        _teacherScopeService = teacherScopeService;
        _aiGradingService = aiGradingService;
        _notificationRealtimeDispatcher = notificationRealtimeDispatcher;
        _logger = logger;
    }

    public async Task<PagedResponse<SubmissionResponse>> GetPagedAsync(SubmissionQueryRequest request, CancellationToken cancellationToken)
    {
        return await ApplyFilters(BuildSubmissionQuery(), request)
            .OrderByDescending(x => x.CreatedAt)
            .ToPagedResponseAsync(ToPaginationRequest(request), cancellationToken, x => x.ToResponse());
    }

    public async Task<PagedResponse<SubmissionResponse>> GetForTeacherUserAsync(Guid teacherUserId, SubmissionQueryRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);

        return await ApplyFilters(_teacherScopeService.ApplySubmissionScope(BuildSubmissionQuery(), teacherId), request)
            .OrderByDescending(x => x.CreatedAt)
            .ToPagedResponseAsync(ToPaginationRequest(request), cancellationToken, x => x.ToResponse());
    }

    public async Task<PagedResponse<SubmissionResponse>> GetForStudentUserAsync(Guid studentUserId, SubmissionQueryRequest request, CancellationToken cancellationToken)
    {
        var studentId = await GetStudentIdByUserIdAsync(studentUserId, cancellationToken);

        return await ApplyFilters(BuildSubmissionQuery().Where(x => x.StudentId == studentId), request)
            .OrderByDescending(x => x.CreatedAt)
            .ToPagedResponseAsync(ToPaginationRequest(request), cancellationToken, x => ToStudentVisibleResponse(x));
    }

    public async Task<SubmissionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var submission = await BuildSubmissionQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Submission not found.", 404);

        return submission.ToResponse();
    }

    public async Task<SubmissionResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        var submission = await _teacherScopeService.ApplySubmissionScope(BuildSubmissionQuery(), teacherId)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Submission not found.", 404);

        return submission.ToResponse();
    }

    public async Task<SubmissionResponse> GetForStudentUserByIdAsync(Guid studentUserId, Guid id, CancellationToken cancellationToken)
    {
        var studentId = await GetStudentIdByUserIdAsync(studentUserId, cancellationToken);
        var submission = await BuildSubmissionQuery()
            .SingleOrDefaultAsync(x => x.Id == id && x.StudentId == studentId, cancellationToken)
            ?? throw new AppException("Submission not found.", 404);

        return ToStudentVisibleResponse(submission);
    }

    public async Task<SubmissionResponse> CreateAsync(Guid submittedByUserId, CreateSubmissionRequest request, CancellationToken cancellationToken)
    {
        if (!request.StudentId.HasValue)
        {
            throw new AppException("StudentId is required for this request.");
        }

        var (student, exam) = await EnsureSubmissionReferencesAsync(request.StudentId.Value, request.ExamId, cancellationToken);
        await EnsureSubmissionIsUniqueAsync(request.StudentId.Value, request.ExamId, cancellationToken);

        var submission = new Submission
        {
            ExamId = exam.Id,
            StudentId = student.Id,
            SubmittedByUserId = submittedByUserId,
            EssayPrompt = request.EssayPrompt?.Trim(),
            AnswerText = request.AnswerText.Trim(),
            MaximumScore = exam.TotalMarks
        };

        _context.Submissions.Add(submission);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Submission created. SubmissionId: {SubmissionId}, StudentId: {StudentId}, ExamId: {ExamId}", submission.Id, submission.StudentId, submission.ExamId);

        return await GetByIdAsync(submission.Id, cancellationToken);
    }

    public async Task<SubmissionResponse> CreateForStudentUserAsync(Guid studentUserId, CreateSubmissionRequest request, CancellationToken cancellationToken)
    {
        var studentId = await GetStudentIdByUserIdAsync(studentUserId, cancellationToken);
        return await CreateAsync(studentUserId, request with { StudentId = studentId }, cancellationToken);
    }

    public async Task<SubmissionResponse> CreateForTeacherUserAsync(Guid teacherUserId, CreateSubmissionRequest request, CancellationToken cancellationToken)
    {
        if (!request.StudentId.HasValue)
        {
            throw new AppException("StudentId is required for this request.");
        }

        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        var (_, exam) = await EnsureSubmissionReferencesAsync(request.StudentId.Value, request.ExamId, cancellationToken);
        await _teacherScopeService.EnsureCanManageClassSubjectAsync(teacherId, exam.ClassId, exam.SubjectId, cancellationToken);

        return await CreateAsync(teacherUserId, request, cancellationToken);
    }

    public async Task<SubmissionResponse> UpdateTeacherReviewAsync(Guid reviewerUserId, Guid id, UpdateSubmissionTeacherReviewRequest request, CancellationToken cancellationToken)
    {
        var submission = await BuildMutableSubmissionQuery()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Submission not found.", 404);

        EnsureTeacherReviewIsValid(submission.MaximumScore, request);

        var releaseJustEnabled = !submission.IsAiFeedbackReleasedToStudent && request.IsAiFeedbackReleasedToStudent;

        submission.TeacherFinalScore = request.TeacherFinalScore;
        submission.TeacherFinalGrade = request.TeacherFinalGrade?.Trim();
        submission.TeacherReviewNotes = request.TeacherReviewNotes?.Trim();
        submission.IsAiFeedbackReleasedToStudent = request.IsAiFeedbackReleasedToStudent;
        submission.ReviewedByUserId = reviewerUserId;
        submission.ReviewedAt = DateTime.UtcNow;

        Guid[] releaseNotificationIds = [];
        if (releaseJustEnabled)
        {
            var notification = new Notification
            {
                UserId = submission.Student!.UserId,
                StudentId = submission.StudentId,
                Title = "Essay feedback available",
                Message = $"Feedback for '{submission.Exam!.Title}' is now available for review."
            };

            _context.Notifications.Add(notification);
            releaseNotificationIds = [notification.Id];
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _notificationRealtimeDispatcher.BroadcastCreatedAsync(releaseNotificationIds, cancellationToken);

        _logger.LogInformation("Teacher review updated for submission {SubmissionId} by user {ReviewerUserId}", submission.Id, reviewerUserId);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SubmissionResponse> UpdateTeacherReviewForTeacherUserAsync(Guid teacherUserId, Guid id, UpdateSubmissionTeacherReviewRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanManageSubmissionAsync(teacherId, id, cancellationToken);
        return await UpdateTeacherReviewAsync(teacherUserId, id, request, cancellationToken);
    }

    public async Task<SubmissionResponse> GenerateAIFeedbackAsync(Guid requestedByUserId, Guid id, RequestSubmissionAIRequest request, CancellationToken cancellationToken)
    {
        return await GenerateAIReviewAsync(requestedByUserId, id, request, AIGradingModes.Feedback, cancellationToken);
    }

    public async Task<SubmissionResponse> GenerateAIFeedbackForTeacherUserAsync(Guid teacherUserId, Guid id, RequestSubmissionAIRequest request, CancellationToken cancellationToken)
    {
        return await GenerateAIReviewForTeacherAsync(teacherUserId, id, request, AIGradingModes.Feedback, cancellationToken);
    }

    public async Task<SubmissionResponse> GenerateSmartGradeAsync(Guid requestedByUserId, Guid id, RequestSubmissionAIRequest request, CancellationToken cancellationToken)
    {
        return await GenerateAIReviewAsync(requestedByUserId, id, request, AIGradingModes.SmartGrade, cancellationToken);
    }

    public async Task<SubmissionResponse> GenerateSmartGradeForTeacherUserAsync(Guid teacherUserId, Guid id, RequestSubmissionAIRequest request, CancellationToken cancellationToken)
    {
        return await GenerateAIReviewForTeacherAsync(teacherUserId, id, request, AIGradingModes.SmartGrade, cancellationToken);
    }

    private async Task<SubmissionResponse> GenerateAIReviewForTeacherAsync(
        Guid teacherUserId,
        Guid id,
        RequestSubmissionAIRequest request,
        string mode,
        CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        await _teacherScopeService.EnsureCanManageSubmissionAsync(teacherId, id, cancellationToken);
        return await GenerateAIReviewAsync(teacherUserId, id, request, mode, cancellationToken);
    }

    private async Task<SubmissionResponse> GenerateAIReviewAsync(
        Guid requestedByUserId,
        Guid id,
        RequestSubmissionAIRequest request,
        string mode,
        CancellationToken cancellationToken)
    {
        var submission = await BuildMutableSubmissionQuery()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Submission not found.", 404);

        var aiResult = await _aiGradingService.GenerateEssayAssessmentAsync(
            new AIEssayAssessmentRequest(
                submission.Id,
                mode,
                submission.Exam!.Title,
                submission.Exam.Subject?.Name ?? string.Empty,
                submission.Exam.Class is null ? string.Empty : $"{submission.Exam.Class.Name} {submission.Exam.Class.Section}",
                submission.MaximumScore,
                submission.EssayPrompt,
                submission.AnswerText,
                request.RubricInstructions,
                request.AdditionalInstructions),
            cancellationToken);

        var isNewReview = submission.AIReview is null;
        submission.AIReview ??= new SubmissionAIReview
        {
            SubmissionId = submission.Id
        };

        if (isNewReview)
        {
            _context.SubmissionAIReviews.Add(submission.AIReview);
        }

        submission.AIReview.RequestedByUserId = requestedByUserId;
        submission.AIReview.Mode = aiResult.Mode;
        submission.AIReview.Model = aiResult.Model;
        submission.AIReview.ProviderResponseId = aiResult.ProviderResponseId;
        submission.AIReview.GrammarScore = aiResult.GrammarScore;
        submission.AIReview.ClarityScore = aiResult.ClarityScore;
        submission.AIReview.StructureScore = aiResult.StructureScore;
        submission.AIReview.ContentScore = aiResult.ContentScore;
        submission.AIReview.OverallSuggestedScore = aiResult.OverallSuggestedScore;
        submission.AIReview.SummaryFeedback = aiResult.SummaryFeedback;
        submission.AIReview.StrengthsJson = JsonSerializer.Serialize(aiResult.Strengths, JsonOptions);
        submission.AIReview.WeaknessesJson = JsonSerializer.Serialize(aiResult.Weaknesses, JsonOptions);
        submission.AIReview.ImprovementsJson = JsonSerializer.Serialize(aiResult.Improvements, JsonOptions);
        submission.AIReview.RubricBreakdownJson = JsonSerializer.Serialize(aiResult.RubricBreakdown, JsonOptions);
        submission.AIReview.SafetyNotes = aiResult.SafetyNotes;
        if (!isNewReview)
        {
            submission.AIReview.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AI review generated. SubmissionId: {SubmissionId}, Mode: {Mode}, RequestedByUserId: {RequestedByUserId}", submission.Id, mode, requestedByUserId);

        return await GetByIdAsync(id, cancellationToken);
    }

    private IQueryable<Submission> BuildSubmissionQuery()
    {
        return _context.Submissions.AsNoTracking()
            .Include(x => x.Exam).ThenInclude(x => x!.Class)
            .Include(x => x.Exam).ThenInclude(x => x!.Subject)
            .Include(x => x.Student).ThenInclude(x => x!.User)
            .Include(x => x.AIReview);
    }

    private IQueryable<Submission> BuildMutableSubmissionQuery()
    {
        return _context.Submissions
            .Include(x => x.Exam).ThenInclude(x => x!.Class)
            .Include(x => x.Exam).ThenInclude(x => x!.Subject)
            .Include(x => x.Student).ThenInclude(x => x!.User)
            .Include(x => x.AIReview);
    }

    private static IQueryable<Submission> ApplyFilters(IQueryable<Submission> query, SubmissionQueryRequest request)
    {
        if (request.ExamId.HasValue)
        {
            query = query.Where(x => x.ExamId == request.ExamId.Value);
        }

        if (request.StudentId.HasValue)
        {
            query = query.Where(x => x.StudentId == request.StudentId.Value);
        }

        if (request.ReleasedOnly == true)
        {
            query = query.Where(x => x.IsAiFeedbackReleasedToStudent);
        }

        return query;
    }

    private static PaginationRequest ToPaginationRequest(SubmissionQueryRequest request)
    {
        return new PaginationRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private SubmissionResponse ToStudentVisibleResponse(Submission submission)
    {
        return submission.ToResponse(includeRestrictedFeedback: submission.IsAiFeedbackReleasedToStudent);
    }

    private async Task<Guid> GetStudentIdByUserIdAsync(Guid studentUserId, CancellationToken cancellationToken)
    {
        var studentId = await _context.Students
            .Where(x => x.UserId == studentUserId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return studentId ?? throw new AppException("Student profile not found.", 404);
    }

    private async Task EnsureSubmissionIsUniqueAsync(Guid studentId, Guid examId, CancellationToken cancellationToken)
    {
        if (await _context.Submissions.AnyAsync(x => x.StudentId == studentId && x.ExamId == examId, cancellationToken))
        {
            throw new AppException("A submission already exists for this student and exam.");
        }
    }

    private async Task<(Student Student, Exam Exam)> EnsureSubmissionReferencesAsync(Guid studentId, Guid examId, CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == studentId, cancellationToken)
            ?? throw new AppException("Student not found.", 404);

        var exam = await _context.Exams
            .Include(x => x.Class)
            .Include(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == examId, cancellationToken)
            ?? throw new AppException("Exam not found.", 404);

        if (!student.ClassId.HasValue || student.ClassId.Value != exam.ClassId)
        {
            throw new AppException("Student does not belong to the selected exam class.");
        }

        return (student, exam);
    }

    private static void EnsureTeacherReviewIsValid(decimal maximumScore, UpdateSubmissionTeacherReviewRequest request)
    {
        if (request.TeacherFinalScore.HasValue && request.TeacherFinalScore.Value > maximumScore)
        {
            throw new AppException("Teacher final score cannot exceed the submission maximum score.");
        }
    }
}
