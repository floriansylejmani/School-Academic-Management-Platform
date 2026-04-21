using FluentValidation;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Timetable;

public sealed record TimetableEntryResponse(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    string DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? RoomNumber,
    DateTime CreatedAt);

public sealed record CreateTimetableEntryRequest(
    Guid ClassId,
    Guid SubjectId,
    Guid TeacherId,
    string DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? RoomNumber);

public sealed record UpdateTimetableEntryRequest(
    Guid ClassId,
    Guid SubjectId,
    Guid TeacherId,
    string DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? RoomNumber);

public interface ITimetableService
{
    Task<PagedResponse<TimetableEntryResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TimetableEntryResponse>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken);
    Task<TimetableEntryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<TimetableEntryResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TimetableEntryResponse>> GetForTeacherUserByClassIdAsync(Guid teacherUserId, Guid classId, CancellationToken cancellationToken);
    Task<TimetableEntryResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken);
    Task<TimetableEntryResponse> CreateAsync(CreateTimetableEntryRequest request, CancellationToken cancellationToken);
    Task<TimetableEntryResponse> UpdateAsync(Guid id, UpdateTimetableEntryRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class CreateTimetableEntryRequestValidator : AbstractValidator<CreateTimetableEntryRequest>
{
    public CreateTimetableEntryRequestValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleFor(x => x.DayOfWeek).NotEmpty();
        RuleFor(x => x.RoomNumber).MaximumLength(30);
    }
}

public sealed class UpdateTimetableEntryRequestValidator : AbstractValidator<UpdateTimetableEntryRequest>
{
    public UpdateTimetableEntryRequestValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleFor(x => x.DayOfWeek).NotEmpty();
        RuleFor(x => x.RoomNumber).MaximumLength(30);
    }
}
