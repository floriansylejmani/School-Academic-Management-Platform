using System.Text.Json;
using SchoolManagement.Application.AI;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Exams;
using SchoolManagement.Application.Fees;
using SchoolManagement.Application.Notifications;
using SchoolManagement.Application.Parents;
using SchoolManagement.Application.Results;
using SchoolManagement.Application.Roles;
using SchoolManagement.Application.Students;
using SchoolManagement.Application.Submissions;
using SchoolManagement.Application.Subjects;
using SchoolManagement.Application.Teachers;
using SchoolManagement.Application.Timetable;
using SchoolManagement.Application.Users;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Persistence.Common;

internal static class MappingExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RoleResponse ToResponse(this Role entity)
        => new(entity.Id, entity.Name, entity.CreatedAt);

    public static UserResponse ToResponse(this User entity)
        => new(
            entity.Id,
            entity.FullName,
            entity.Email,
            entity.Phone,
            entity.Address,
            entity.IsActive,
            entity.RoleId,
            entity.Role?.Name ?? string.Empty,
            entity.ProfilePictureUrl,
            entity.CreatedAt);

    public static StudentResponse ToResponse(this Student entity)
        => new(
            entity.Id,
            entity.UserId,
            entity.User?.FullName ?? string.Empty,
            entity.User?.Email ?? string.Empty,
            entity.User?.Phone,
            entity.StudentCode,
            entity.DateOfBirth,
            entity.Gender,
            entity.AdmissionDate,
            entity.ParentId,
            entity.Parent?.User?.FullName,
            entity.ClassId,
            entity.Class is null ? null : $"{entity.Class.Name} {entity.Class.Section}",
            entity.CreatedAt);

    public static ParentResponse ToResponse(this Parent entity)
        => new(
            entity.Id,
            entity.UserId,
            entity.User?.FullName ?? string.Empty,
            entity.User?.Email ?? string.Empty,
            entity.User?.Phone,
            entity.User?.Address,
            entity.Occupation,
            entity.Students.Count,
            entity.CreatedAt);

    public static TeacherResponse ToResponse(this Teacher entity)
        => new(
            entity.Id,
            entity.UserId,
            entity.User?.FullName ?? string.Empty,
            entity.User?.Email ?? string.Empty,
            entity.User?.Phone,
            entity.TeacherCode,
            entity.Specialization,
            entity.HireDate,
            entity.CreatedAt);

    public static ClassResponse ToResponse(this AcademicClass entity)
        => new(
            entity.Id,
            entity.Name,
            entity.Section,
            entity.AcademicYear,
            entity.ClassTeacherId,
            entity.ClassTeacher?.User?.FullName,
            entity.Students.Count,
            entity.CreatedAt);

    public static SubjectResponse ToResponse(this Subject entity)
        => new(entity.Id, entity.Name, entity.Code, entity.Description, entity.CreatedAt);

    public static AttendanceResponse ToResponse(this AttendanceRecord entity)
        => new(
            entity.Id,
            entity.StudentId,
            entity.Student?.User?.FullName ?? string.Empty,
            entity.ClassId,
            entity.Class is null ? string.Empty : $"{entity.Class.Name} {entity.Class.Section}",
            entity.SubjectId,
            entity.Subject?.Name ?? string.Empty,
            entity.TeacherId,
            entity.Teacher?.User?.FullName ?? string.Empty,
            entity.Date,
            entity.Status.ToString(),
            entity.Remarks,
            entity.CreatedAt);

    public static ExamResponse ToResponse(this Exam entity)
        => new(
            entity.Id,
            entity.ClassId,
            entity.Class is null ? string.Empty : $"{entity.Class.Name} {entity.Class.Section}",
            entity.SubjectId,
            entity.Subject?.Name ?? string.Empty,
            entity.Title,
            entity.ExamDate,
            entity.TotalMarks,
            entity.CreatedAt);

    public static SubmissionResponse ToResponse(this Submission entity, bool includeRestrictedFeedback = true)
        => new(
            entity.Id,
            entity.ExamId,
            entity.Exam?.Title ?? string.Empty,
            entity.StudentId,
            entity.Student?.User?.FullName ?? string.Empty,
            entity.Exam?.ClassId ?? Guid.Empty,
            entity.Exam?.Class is null ? string.Empty : $"{entity.Exam.Class.Name} {entity.Exam.Class.Section}",
            entity.Exam?.SubjectId ?? Guid.Empty,
            entity.Exam?.Subject?.Name ?? string.Empty,
            entity.EssayPrompt,
            entity.AnswerText,
            entity.MaximumScore,
            includeRestrictedFeedback ? entity.TeacherFinalScore : null,
            includeRestrictedFeedback ? entity.TeacherFinalGrade : null,
            includeRestrictedFeedback ? entity.TeacherReviewNotes : null,
            entity.IsAiFeedbackReleasedToStudent,
            entity.CreatedAt,
            includeRestrictedFeedback ? entity.ReviewedAt : null,
            includeRestrictedFeedback && entity.AIReview is not null,
            includeRestrictedFeedback ? entity.AIReview?.ToResponse() : null);

    public static SubmissionAIReviewResponse ToResponse(this SubmissionAIReview entity)
        => new(
            entity.Mode,
            entity.Model,
            entity.CreatedAt,
            entity.GrammarScore,
            entity.ClarityScore,
            entity.StructureScore,
            entity.ContentScore,
            entity.OverallSuggestedScore,
            entity.SummaryFeedback,
            ParseStringArray(entity.StrengthsJson),
            ParseStringArray(entity.WeaknessesJson),
            ParseStringArray(entity.ImprovementsJson),
            ParseRubricBreakdown(entity.RubricBreakdownJson),
            entity.SafetyNotes);

    public static ResultResponse ToResponse(this Result entity)
        => new(
            entity.Id,
            entity.ExamId,
            entity.Exam?.Title ?? string.Empty,
            entity.StudentId,
            entity.Student?.User?.FullName ?? string.Empty,
            entity.Exam?.ClassId ?? Guid.Empty,
            entity.Exam?.Class is null ? string.Empty : $"{entity.Exam.Class.Name} {entity.Exam.Class.Section}",
            entity.Exam?.SubjectId ?? Guid.Empty,
            entity.Exam?.Subject?.Name ?? string.Empty,
            entity.MarksObtained,
            entity.Exam?.TotalMarks ?? 0,
            entity.Grade,
            entity.Remarks,
            entity.CreatedAt);

    public static TimetableEntryResponse ToResponse(this TimetableEntry entity)
        => new(
            entity.Id,
            entity.ClassId,
            entity.Class is null ? string.Empty : $"{entity.Class.Name} {entity.Class.Section}",
            entity.SubjectId,
            entity.Subject?.Name ?? string.Empty,
            entity.TeacherId,
            entity.Teacher?.User?.FullName ?? string.Empty,
            entity.DayOfWeek.ToString(),
            entity.StartTime,
            entity.EndTime,
            entity.RoomNumber,
            entity.CreatedAt);

    public static NotificationResponse ToResponse(this Notification entity)
        => new(entity.Id, entity.UserId, entity.Title, entity.Message, entity.IsRead, entity.CreatedAt,
            entity.StudentId, entity.Student?.User?.FullName);

    public static FeeResponse ToResponse(this Fee entity)
        => new(
            entity.Id,
            entity.StudentId,
            entity.Student?.User?.FullName ?? string.Empty,
            entity.FeeType,
            entity.Amount,
            entity.DueDate,
            entity.Status.ToString(),
            entity.Payments.Select(x => x.ToResponse()).ToArray(),
            entity.CreatedAt);

    public static PaymentResponse ToResponse(this Payment entity)
        => new(
            entity.Id,
            entity.FeeId,
            entity.Fee?.FeeType ?? string.Empty,
            entity.Fee?.StudentId ?? Guid.Empty,
            entity.Fee?.Student?.User?.FullName ?? string.Empty,
            entity.AmountPaid,
            entity.PaymentDate,
            entity.PaymentMethod,
            entity.TransactionReference);

    public static FeeStatus CalculateFeeStatus(this Fee fee)
    {
        var paid = fee.Payments.Sum(x => x.AmountPaid);

        if (paid >= fee.Amount)
        {
            return FeeStatus.Paid;
        }

        if (paid > 0)
        {
            return FeeStatus.PartiallyPaid;
        }

        return fee.DueDate < DateOnly.FromDateTime(DateTime.UtcNow) ? FeeStatus.Overdue : FeeStatus.Pending;
    }

    private static IReadOnlyCollection<string> ParseStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
    }

    private static IReadOnlyCollection<AIRubricBreakdownItem> ParseRubricBreakdown(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<AIRubricBreakdownItem[]>(json, JsonOptions) ?? [];
    }
}
