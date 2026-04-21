using FluentValidation;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Results;

public sealed record ResultResponse(
    Guid Id,
    Guid ExamId,
    string ExamTitle,
    Guid StudentId,
    string StudentName,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    decimal MarksObtained,
    decimal TotalMarks,
    string Grade,
    string? Remarks,
    DateTime CreatedAt);

public sealed record CreateResultRequest(Guid ExamId, Guid StudentId, decimal MarksObtained, string Grade, string? Remarks);

public sealed record UpdateResultRequest(Guid ExamId, Guid StudentId, decimal MarksObtained, string Grade, string? Remarks);

public interface IResultService
{
    Task<PagedResponse<ResultResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ResultResponse>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken);
    Task<ResultResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<ResultResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ResultResponse>> GetForTeacherUserByStudentIdAsync(Guid teacherUserId, Guid studentId, CancellationToken cancellationToken);
    Task<ResultResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken);
    Task<ResultResponse> CreateForTeacherUserAsync(Guid teacherUserId, CreateResultRequest request, CancellationToken cancellationToken);
    Task<ResultResponse> UpdateForTeacherUserAsync(Guid teacherUserId, Guid id, UpdateResultRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteForTeacherUserAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken);
    Task<ResultResponse> CreateAsync(CreateResultRequest request, CancellationToken cancellationToken);
    Task<ResultResponse> UpdateAsync(Guid id, UpdateResultRequest request, CancellationToken cancellationToken);
}

public sealed class CreateResultRequestValidator : AbstractValidator<CreateResultRequest>
{
    public CreateResultRequestValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.MarksObtained).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Grade).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Remarks).MaximumLength(250);
    }
}

public sealed class UpdateResultRequestValidator : AbstractValidator<UpdateResultRequest>
{
    public UpdateResultRequestValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.MarksObtained).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Grade).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Remarks).MaximumLength(250);
    }
}
