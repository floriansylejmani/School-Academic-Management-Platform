using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Roles;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Admin")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<RoleResponse>>>> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResponse<RoleResponse>>.Ok(await _roleService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<RoleResponse>.Ok(await _roleService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var response = await _roleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<RoleResponse>.Ok(response, "Role created successfully."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> Update(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<RoleResponse>.Ok(await _roleService.UpdateAsync(id, request, cancellationToken), "Role updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Role deleted successfully."));
    }
}
