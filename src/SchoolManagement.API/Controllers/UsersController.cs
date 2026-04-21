using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Users;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserResponse>>>> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResponse<UserResponse>>.Ok(await _userService.GetPagedAsync(request, cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<UserResponse>.Ok(await _userService.GetByIdAsync(id, cancellationToken)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await _userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<UserResponse>.Ok(response, "User created successfully."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<UserResponse>.Ok(await _userService.UpdateAsync(id, request, cancellationToken), "User updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "User deleted successfully."));
    }
}
