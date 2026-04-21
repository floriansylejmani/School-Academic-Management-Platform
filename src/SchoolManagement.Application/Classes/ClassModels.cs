using FluentValidation;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Classes;

public sealed record ClassResponse(
    Guid Id,
    string Name,
    string Section,
    string AcademicYear,
    Guid? ClassTeacherId,
    string? ClassTeacherName,
    int StudentCount,
    DateTime CreatedAt);

public sealed record CreateClassRequest(string Name, string Section, string AcademicYear, Guid? ClassTeacherId);

public sealed record UpdateClassRequest(string Name, string Section, string AcademicYear, Guid? ClassTeacherId);

public interface IClassService
{
    Task<PagedResponse<ClassResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<ClassResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<ClassResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken);
    Task<ClassResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken);
    Task<ClassResponse> CreateAsync(CreateClassRequest request, CancellationToken cancellationToken);
    Task<ClassResponse> UpdateAsync(Guid id, UpdateClassRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class CreateClassRequestValidator : AbstractValidator<CreateClassRequest>
{
    public CreateClassRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Section).NotEmpty().MaximumLength(20);
        RuleFor(x => x.AcademicYear).NotEmpty().MaximumLength(20);
    }
}

public sealed class UpdateClassRequestValidator : AbstractValidator<UpdateClassRequest>
{
    public UpdateClassRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Section).NotEmpty().MaximumLength(20);
        RuleFor(x => x.AcademicYear).NotEmpty().MaximumLength(20);
    }
}
