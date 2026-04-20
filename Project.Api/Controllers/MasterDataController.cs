using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/master-data")]
[Produces("application/json")]
//[Authorize]
public sealed class MasterDataController : ControllerBase
{
    private readonly IMasterDataService _service;

    public MasterDataController(IMasterDataService service)
    {
        _service = service;
    }

    [HttpGet("sections")]
    public async Task<IActionResult> GetSections([FromQuery] SectionFilterParams filters)
        => Ok(await _service.GetSectionsAsync(filters));

    [HttpPost("sections")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateSection([FromBody] SaveSectionRequest request)
        => Ok(await _service.CreateSectionAsync(request));

    [HttpPut("sections/{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateSection(long id, [FromBody] SaveSectionRequest request)
    {
        var result = await _service.UpdateSectionAsync(id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpGet("positions")]
    public async Task<IActionResult> GetPositions([FromQuery] PositionFilterParams filters)
        => Ok(await _service.GetPositionsAsync(filters));

    [HttpPost("positions")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreatePosition([FromBody] SavePositionRequest request)
        => Ok(await _service.CreatePositionAsync(request));

    [HttpPut("positions/{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdatePosition(long id, [FromBody] SavePositionRequest request)
    {
        var result = await _service.UpdatePositionAsync(id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpGet("locations")]
    public async Task<IActionResult> GetLocations([FromQuery] LocationFilterParams filters)
        => Ok(await _service.GetLocationsAsync(filters));

    [HttpPost("locations")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateLocation([FromBody] SaveLocationRequest request)
        => Ok(await _service.CreateLocationAsync(request));

    [HttpPut("locations/{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateLocation(long id, [FromBody] SaveLocationRequest request)
    {
        var result = await _service.UpdateLocationAsync(id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees([FromQuery] EmployeeFilterParams filters)
        => Ok(await _service.GetEmployeesAsync(filters));

    [HttpGet("employees/options")]
    public async Task<IActionResult> GetEmployeeOptions([FromQuery] string? search, [FromQuery] int top = 20)
        => Ok(await _service.GetEmployeeOptionsAsync(search, top));

    [HttpGet("employees/{id:long}")]
    public async Task<IActionResult> GetEmployeeById(long id)
    {
        var result = await _service.GetEmployeeByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("employees")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateEmployee([FromBody] SaveEmployeeRequest request)
        => Ok(await _service.CreateEmployeeAsync(request));

    [HttpPut("employees/{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateEmployee(long id, [FromBody] SaveEmployeeRequest request)
    {
        var result = await _service.UpdateEmployeeAsync(id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }
}
