using FluentValidation;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Subjects;

public sealed record SubjectResponse(Guid Id, string Name, string Code, string? Description, DateTime CreatedAt);

public sealed record CreateSubjectRequest(string Name, string Code, string? Description);

public sealed record UpdateSubjectRequest(string Name, string Code, string? Description);

public interface ISubjectService
{
    Task<PagedResponse<SubjectResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<SubjectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<SubjectResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken);
    Task<SubjectResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken);
    Task<SubjectResponse> CreateAsync(CreateSubjectRequest request, CancellationToken cancellationToken);
    Task<SubjectResponse> UpdateAsync(Guid id, UpdateSubjectRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class CreateSubjectRequestValidator : AbstractValidator<CreateSubjectRequest>
{
    public CreateSubjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateSubjectRequestValidator : AbstractValidator<UpdateSubjectRequest>
{
    public UpdateSubjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
