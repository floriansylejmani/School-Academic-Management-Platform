using FluentValidation;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Attendance;

public sealed record AttendanceResponse(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    DateOnly Date,
    string Status,
    string? Remarks,
    DateTime CreatedAt);

public sealed record CreateAttendanceRequest(
    Guid StudentId,
    Guid ClassId,
    Guid SubjectId,
    Guid TeacherId,
    DateOnly Date,
    string Status,
    string? Remarks);

public sealed record UpdateAttendanceRequest(
    Guid StudentId,
    Guid ClassId,
    Guid SubjectId,
    Guid TeacherId,
    DateOnly Date,
    string Status,
    string? Remarks);

public interface IAttendanceService
{
    Task<PagedResponse<AttendanceResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AttendanceResponse>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken);
    Task<AttendanceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<AttendanceResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AttendanceResponse>> GetForTeacherUserByStudentIdAsync(Guid teacherUserId, Guid studentId, CancellationToken cancellationToken);
    Task<AttendanceResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken);
    Task<AttendanceResponse> CreateForTeacherUserAsync(Guid teacherUserId, CreateAttendanceRequest request, CancellationToken cancellationToken);
    Task<AttendanceResponse> UpdateForTeacherUserAsync(Guid teacherUserId, Guid id, UpdateAttendanceRequest request, CancellationToken cancellationToken);
    Task<AttendanceResponse> CreateAsync(CreateAttendanceRequest request, CancellationToken cancellationToken);
    Task<AttendanceResponse> UpdateAsync(Guid id, UpdateAttendanceRequest request, CancellationToken cancellationToken);
}

public sealed class CreateAttendanceRequestValidator : AbstractValidator<CreateAttendanceRequest>
{
    public CreateAttendanceRequestValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleFor(x => x.Date).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Remarks).MaximumLength(250);
    }
}

public sealed class UpdateAttendanceRequestValidator : AbstractValidator<UpdateAttendanceRequest>
{
    public UpdateAttendanceRequestValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleFor(x => x.Date).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Remarks).MaximumLength(250);
    }
}
