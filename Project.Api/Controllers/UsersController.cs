using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
//[Authorize(Roles = "admin")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] UserFilterParams filters)
        => Ok(await _userService.GetAllAsync(filters));

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions([FromQuery] UserOptionFilterParams filters)
        => Ok(await _userService.GetOptionsAsync(filters));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _userService.GetByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateAsync(request);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.UserId }, result) : BadRequest(result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserRequest request)
    {
        var result = await _userService.UpdateAsync(id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpPost("{id:long}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetPasswordRequest request)
    {
        var result = await _userService.ResetPasswordAsync(id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _userService.DeleteAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
