using FluentValidation;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Roles;

public sealed record RoleResponse(Guid Id, string Name, DateTime CreatedAt);

public sealed record CreateRoleRequest(string Name);

public sealed record UpdateRoleRequest(string Name);

public interface IRoleService
{
    Task<PagedResponse<RoleResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<RoleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken);
    Task<RoleResponse> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}
