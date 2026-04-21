using FluentValidation;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Exams;

public sealed record ExamResponse(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    string Title,
    DateOnly ExamDate,
    decimal TotalMarks,
    DateTime CreatedAt);

public sealed record CreateExamRequest(Guid ClassId, Guid SubjectId, string Title, DateOnly ExamDate, decimal TotalMarks);

public sealed record UpdateExamRequest(Guid ClassId, Guid SubjectId, string Title, DateOnly ExamDate, decimal TotalMarks);

public interface IExamService
{
    Task<PagedResponse<ExamResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ExamResponse>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken);
    Task<ExamResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<ExamResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ExamResponse>> GetForTeacherUserByClassIdAsync(Guid teacherUserId, Guid classId, CancellationToken cancellationToken);
    Task<ExamResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken);
    Task<ExamResponse> CreateForTeacherUserAsync(Guid teacherUserId, CreateExamRequest request, CancellationToken cancellationToken);
    Task<ExamResponse> UpdateForTeacherUserAsync(Guid teacherUserId, Guid id, UpdateExamRequest request, CancellationToken cancellationToken);
    Task DeleteForTeacherUserAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken);
    Task<ExamResponse> CreateAsync(CreateExamRequest request, CancellationToken cancellationToken);
    Task<ExamResponse> UpdateAsync(Guid id, UpdateExamRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class CreateExamRequestValidator : AbstractValidator<CreateExamRequest>
{
    public CreateExamRequestValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ExamDate).GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1));
        RuleFor(x => x.TotalMarks).GreaterThan(0);
    }
}

public sealed class UpdateExamRequestValidator : AbstractValidator<UpdateExamRequest>
{
    public UpdateExamRequestValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ExamDate).GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1));
        RuleFor(x => x.TotalMarks).GreaterThan(0);
    }
}
