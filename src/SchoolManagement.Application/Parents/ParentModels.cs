using FluentValidation;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Parents;

public sealed record ParentResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    string? Occupation,
    int StudentsCount,
    DateTime CreatedAt);

public sealed record CreateParentRequest(
    string FullName,
    string Email,
    string Password,
    string? Phone,
    string? Address,
    string? Occupation);

public sealed record UpdateParentRequest(
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    string? Occupation);

public interface IParentService
{
    Task<PagedResponse<ParentResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<ParentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ParentResponse> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<ParentResponse> CreateAsync(CreateParentRequest request, CancellationToken cancellationToken);
    Task<ParentResponse> UpdateAsync(Guid id, UpdateParentRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class CreateParentRequestValidator : AbstractValidator<CreateParentRequest>
{
    public CreateParentRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Address).MaximumLength(250);
        RuleFor(x => x.Occupation).MaximumLength(100);
    }
}

public sealed class UpdateParentRequestValidator : AbstractValidator<UpdateParentRequest>
{
    public UpdateParentRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Address).MaximumLength(250);
        RuleFor(x => x.Occupation).MaximumLength(100);
    }
}
