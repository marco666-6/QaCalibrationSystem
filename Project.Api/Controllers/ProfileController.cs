using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Produces("application/json")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _userService.GetMyProfileAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateMyProfileRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _userService.UpdateMyProfileAsync(userId, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    private bool TryGetUserId(out long userId) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
