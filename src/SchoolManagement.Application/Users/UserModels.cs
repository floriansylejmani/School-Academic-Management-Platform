using FluentValidation;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Users;

public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    bool IsActive,
    Guid RoleId,
    string Role,
    string? ProfilePictureUrl,
    DateTime CreatedAt);

public sealed record CreateUserRequest(
    Guid RoleId,
    string FullName,
    string Email,
    string Password,
    string? Phone,
    string? Address,
    bool IsActive);

public sealed record UpdateUserRequest(
    Guid RoleId,
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    bool IsActive);

public interface IUserService
{
    Task<PagedResponse<UserResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Address).MaximumLength(250);
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Address).MaximumLength(250);
    }
}
