using FluentValidation;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Teachers;

public sealed record TeacherResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    string TeacherCode,
    string Specialization,
    DateOnly HireDate,
    DateTime CreatedAt);

public sealed record CreateTeacherRequest(
    string FullName,
    string Email,
    string Password,
    string? Phone,
    string? Address,
    string TeacherCode,
    string Specialization,
    DateOnly HireDate);

public sealed record UpdateTeacherRequest(
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    string TeacherCode,
    string Specialization,
    DateOnly HireDate);

public interface ITeacherService
{
    Task<PagedResponse<TeacherResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<TeacherResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TeacherResponse> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<TeacherResponse> CreateAsync(CreateTeacherRequest request, CancellationToken cancellationToken);
    Task<TeacherResponse> UpdateAsync(Guid id, UpdateTeacherRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class CreateTeacherRequestValidator : AbstractValidator<CreateTeacherRequest>
{
    public CreateTeacherRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.TeacherCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Specialization).NotEmpty().MaximumLength(100);
        RuleFor(x => x.HireDate).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}

public sealed class UpdateTeacherRequestValidator : AbstractValidator<UpdateTeacherRequest>
{
    public UpdateTeacherRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.TeacherCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Specialization).NotEmpty().MaximumLength(100);
        RuleFor(x => x.HireDate).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
