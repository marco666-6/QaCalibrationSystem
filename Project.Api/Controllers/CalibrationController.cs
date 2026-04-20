using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/calibration")]
[Produces("application/json")]
//[Authorize]
public sealed class CalibrationController : ControllerBase
{
    private readonly ICalibrationService _service;

    public CalibrationController(ICalibrationService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int? year, [FromQuery] long? sectionId, [FromQuery] string? status)
        => Ok(await _service.GetDashboardOverviewAsync(year, sectionId, status));

    [HttpGet("approvers")]
    public async Task<IActionResult> GetApprovers([FromQuery] ApproverFilterParams filters)
        => Ok(await _service.GetApproversAsync(filters));

    [HttpPost("approvers")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateApprover([FromBody] SaveApproverRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        return Ok(await _service.CreateApproverAsync(request, employeeCode));
    }

    [HttpPut("approvers/{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateApprover(long id, [FromBody] SaveApproverRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.UpdateApproverAsync(id, request, employeeCode);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpGet("equipments/summary-cards")]
    public async Task<IActionResult> GetEquipmentSummaryCards()
        => Ok(await _service.GetEquipmentSummaryCardsAsync());

    [HttpGet("equipments/group-summary")]
    public async Task<IActionResult> GetEquipmentGroupSummary([FromQuery] string? search, [FromQuery] long? sectionId, [FromQuery] string? dueCategory)
        => Ok(await _service.GetEquipmentGroupSummaryAsync(search, sectionId, dueCategory));

    [HttpGet("equipments")]
    public async Task<IActionResult> GetEquipments([FromQuery] EquipmentFilterParams filters)
        => Ok(await _service.GetEquipmentsAsync(filters));

    [HttpGet("equipments/{id:long}")]
    public async Task<IActionResult> GetEquipmentById(long id)
    {
        var result = await _service.GetEquipmentByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("equipments")]
    public async Task<IActionResult> CreateEquipment([FromBody] SaveEquipmentRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.CreateEquipmentAsync(request, employeeCode);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("equipments/{id:long}")]
    public async Task<IActionResult> UpdateEquipment(long id, [FromBody] SaveEquipmentRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.UpdateEquipmentAsync(id, request, employeeCode);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpPost("equipments/bulk-status")]
    public async Task<IActionResult> BulkChangeStatus([FromBody] BulkChangeEquipmentStatusRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.BulkChangeEquipmentStatusAsync(request, employeeCode);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("equipments/bulk-section")]
    public async Task<IActionResult> BulkMoveSection([FromBody] BulkMoveEquipmentSectionRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.BulkMoveEquipmentSectionAsync(request, employeeCode);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("equipments/bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteEquipmentsRequest request)
    {
        var result = await _service.BulkDeleteEquipmentsAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("reminders")]
    public async Task<IActionResult> GetReminders([FromQuery] ReminderFilterParams filters)
        => Ok(await _service.GetRemindersAsync(filters));

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans([FromQuery] PlanFilterParams filters)
        => Ok(await _service.GetPlansAsync(filters));

    [HttpGet("plans/{id:long}")]
    public async Task<IActionResult> GetPlanById(long id)
    {
        var result = await _service.GetPlanByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.CreatePlanAsync(request, employeeCode);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("plans/{id:long}")]
    public async Task<IActionResult> UpdatePlanHeader(long id, [FromBody] UpdatePlanHeaderRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.UpdatePlanHeaderAsync(id, request, employeeCode);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("plans/{id:long}/submit")]
    public async Task<IActionResult> SubmitPlan(long id)
    {
        var result = await _service.SubmitPlanAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("plans/{id:long}/approval")]
    public async Task<IActionResult> ActOnPlanApproval(long id, [FromBody] ApprovalActionRequest request)
    {
        if (!TryGetEmployeeId(out var employeeId) || !TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Required employee claims are missing."));

        var result = await _service.ActOnPlanApprovalAsync(id, employeeId, request, employeeCode);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("plans/{id:long}")]
    public async Task<IActionResult> DeletePlan(long id)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.DeletePlanAsync(id, employeeCode);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpPost("plans/{id:long}/start-actual")]
    public async Task<IActionResult> StartActualFromPlan(long id, [FromBody] CreateActualRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.StartActualFromPlanAsync(id, request with { PlanId = id }, employeeCode);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("actuals")]
    public async Task<IActionResult> GetActuals([FromQuery] ActualFilterParams filters)
        => Ok(await _service.GetActualsAsync(filters));

    [HttpGet("actuals/{id:long}")]
    public async Task<IActionResult> GetActualById(long id)
    {
        var result = await _service.GetActualByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("actuals")]
    public async Task<IActionResult> CreateActual([FromBody] CreateActualRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.CreateActualAsync(request, employeeCode);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("actuals/{id:long}")]
    public async Task<IActionResult> UpdateActualHeader(long id, [FromBody] UpdateActualHeaderRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.UpdateActualHeaderAsync(id, request, employeeCode);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("actuals/{id:long}/workers")]
    public async Task<IActionResult> AddWorker(long id, [FromBody] SaveActualWorkerRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.AddWorkerAsync(id, request, employeeCode);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpPut("actual-items/{itemId:long}")]
    public async Task<IActionResult> UpdateActualItem(long itemId, [FromBody] UpdateActualItemRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.UpdateActualItemAsync(itemId, request, employeeCode);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("actual-item-details/{detailId:long}")]
    public async Task<IActionResult> UpdateActualItemDetail(long detailId, [FromBody] UpdateActualItemDetailRequest request)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.UpdateActualItemDetailAsync(detailId, request, employeeCode);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpPost("actuals/{id:long}/complete")]
    public async Task<IActionResult> CompleteActual(long id)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.CompleteActualAsync(id, employeeCode);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("actuals/{id:long}/submit")]
    public async Task<IActionResult> SubmitActual(long id)
    {
        var result = await _service.SubmitActualAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("actuals/{id:long}/approval")]
    public async Task<IActionResult> ActOnActualApproval(long id, [FromBody] ApprovalActionRequest request)
    {
        if (!TryGetEmployeeId(out var employeeId) || !TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Required employee claims are missing."));

        var result = await _service.ActOnActualApprovalAsync(id, employeeId, request, employeeCode);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("actuals/{id:long}")]
    public async Task<IActionResult> DeleteActual(long id)
    {
        if (!TryGetEmployeeCode(out var employeeCode))
            return Unauthorized(ApiResponse.Fail("Employee code claim is missing."));

        var result = await _service.DeleteActualAsync(id, employeeCode);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    private bool TryGetEmployeeId(out long employeeId) => long.TryParse(User.FindFirstValue("employee_id"), out employeeId);
    private bool TryGetEmployeeCode(out string employeeCode)
    {
        employeeCode = User.FindFirstValue("employee_code") ?? string.Empty;
        return !string.IsNullOrWhiteSpace(employeeCode);
    }
}
