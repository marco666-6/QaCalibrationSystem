using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;

namespace Project.Application.Services;

public sealed class CalibrationService : ICalibrationService
{
    private readonly ICalibrationRepository _repository;
    private readonly IValidator<SaveApproverRequest> _approverValidator;
    private readonly IValidator<SaveEquipmentRequest> _equipmentValidator;
    private readonly IValidator<BulkChangeEquipmentStatusRequest> _bulkStatusValidator;
    private readonly IValidator<BulkMoveEquipmentSectionRequest> _bulkSectionValidator;
    private readonly IValidator<BulkDeleteEquipmentsRequest> _bulkDeleteValidator;
    private readonly IValidator<CreatePlanRequest> _createPlanValidator;
    private readonly IValidator<CreateActualRequest> _createActualValidator;
    private readonly IValidator<ApprovalActionRequest> _approvalActionValidator;
    private readonly IValidator<SaveActualWorkerRequest> _workerValidator;
    private readonly IValidator<UpdateActualItemDetailRequest> _actualItemDetailValidator;

    public CalibrationService(
        ICalibrationRepository repository,
        IValidator<SaveApproverRequest> approverValidator,
        IValidator<SaveEquipmentRequest> equipmentValidator,
        IValidator<BulkChangeEquipmentStatusRequest> bulkStatusValidator,
        IValidator<BulkMoveEquipmentSectionRequest> bulkSectionValidator,
        IValidator<BulkDeleteEquipmentsRequest> bulkDeleteValidator,
        IValidator<CreatePlanRequest> createPlanValidator,
        IValidator<CreateActualRequest> createActualValidator,
        IValidator<ApprovalActionRequest> approvalActionValidator,
        IValidator<SaveActualWorkerRequest> workerValidator,
        IValidator<UpdateActualItemDetailRequest> actualItemDetailValidator)
    {
        _repository = repository;
        _approverValidator = approverValidator;
        _equipmentValidator = equipmentValidator;
        _bulkStatusValidator = bulkStatusValidator;
        _bulkSectionValidator = bulkSectionValidator;
        _bulkDeleteValidator = bulkDeleteValidator;
        _createPlanValidator = createPlanValidator;
        _createActualValidator = createActualValidator;
        _approvalActionValidator = approvalActionValidator;
        _workerValidator = workerValidator;
        _actualItemDetailValidator = actualItemDetailValidator;
    }

    public async Task<ApiResponse<DashboardOverviewDto>> GetDashboardOverviewAsync(int? year, long? sectionId, string? status)
        => ApiResponse<DashboardOverviewDto>.Ok(await _repository.GetDashboardOverviewAsync(year, sectionId, status));

    public async Task<ApiResponse<PagedResult<ApproverDto>>> GetApproversAsync(ApproverFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetApproversAsync(filters);
        return ApiResponse<PagedResult<ApproverDto>>.Ok(PagedResult<ApproverDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<ApproverDto>> CreateApproverAsync(SaveApproverRequest request, string createdBy)
    {
        var validation = await _approverValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<ApproverDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.GetEmployeeByIdAsync(request.EmployeeId) is null)
            return ApiResponse<ApproverDto>.Fail("Selected employee was not found.");

        if (await _repository.ApproverAssignmentExistsAsync(request.EmployeeId, request.StepNo))
            return ApiResponse<ApproverDto>.Fail("This employee is already assigned to the selected approval step.");

        var entity = new CalibrationApprover
        {
            EmployeeId = request.EmployeeId,
            StepNo = request.StepNo,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        entity.Id = await _repository.CreateApproverAsync(entity);
        var created = await _repository.GetApproverByIdAsync(entity.Id);
        return ApiResponse<ApproverDto>.Created(created!);
    }

    public async Task<ApiResponse<ApproverDto>> UpdateApproverAsync(long approverId, SaveApproverRequest request, string updatedBy)
    {
        var validation = await _approverValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<ApproverDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var existing = await _repository.GetApproverByIdAsync(approverId);
        if (existing is null)
            return ApiResponse<ApproverDto>.NotFound("Approver not found.");

        if (await _repository.GetEmployeeByIdAsync(request.EmployeeId) is null)
            return ApiResponse<ApproverDto>.Fail("Selected employee was not found.");

        if (await _repository.ApproverAssignmentExistsAsync(request.EmployeeId, request.StepNo, approverId))
            return ApiResponse<ApproverDto>.Fail("This employee is already assigned to the selected approval step.");

        var entity = new CalibrationApprover
        {
            Id = approverId,
            EmployeeId = request.EmployeeId,
            StepNo = request.StepNo,
            IsActive = request.IsActive,
            CreatedAt = existing.CreatedAt,
            CreatedBy = existing.CreatedBy,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = updatedBy
        };

        await _repository.UpdateApproverAsync(entity);
        var updated = await _repository.GetApproverByIdAsync(approverId);
        return ApiResponse<ApproverDto>.Ok(updated!, "Updated successfully.");
    }

    public async Task<ApiResponse<EquipmentSummaryCardsDto>> GetEquipmentSummaryCardsAsync()
        => ApiResponse<EquipmentSummaryCardsDto>.Ok(await _repository.GetEquipmentSummaryCardsAsync());

    public async Task<ApiResponse<IEnumerable<EquipmentGroupSummaryDto>>> GetEquipmentGroupSummaryAsync(string? search, long? sectionId, string? dueCategory)
        => ApiResponse<IEnumerable<EquipmentGroupSummaryDto>>.Ok(await _repository.GetEquipmentGroupSummaryAsync(search, sectionId, dueCategory));

    public async Task<ApiResponse<PagedResult<EquipmentListItemDto>>> GetEquipmentsAsync(EquipmentFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetEquipmentsAsync(filters);
        return ApiResponse<PagedResult<EquipmentListItemDto>>.Ok(PagedResult<EquipmentListItemDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<EquipmentDetailDto>> GetEquipmentByIdAsync(long equipmentId)
    {
        var item = await _repository.GetEquipmentByIdAsync(equipmentId);
        return item is null ? ApiResponse<EquipmentDetailDto>.NotFound("Equipment not found.") : ApiResponse<EquipmentDetailDto>.Ok(item);
    }

    public async Task<ApiResponse<EquipmentListItemDto>> CreateEquipmentAsync(SaveEquipmentRequest request, string createdBy)
    {
        var validation = await _equipmentValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<EquipmentListItemDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.EquipmentControlNoExistsAsync(request.ControlNo.Trim()))
            return ApiResponse<EquipmentListItemDto>.Fail($"Control number '{request.ControlNo}' is already in use.");

        var pic = await _repository.GetEmployeeByIdAsync(request.PicId);
        if (pic is null)
            return ApiResponse<EquipmentListItemDto>.Fail("Selected PIC was not found.");

        var entity = new CalibrationEquipment
        {
            EquipmentName = request.EquipmentName.Trim(),
            ControlNo = request.ControlNo.Trim(),
            SerialNo = request.SerialNo?.Trim(),
            Brand = request.Brand?.Trim(),
            Model = request.Model?.Trim(),
            Location = request.Location.Trim(),
            SectionId = request.SectionId,
            PicId = request.PicId,
            PicCode = pic.EmployeeCode,
            PicFullName = pic.FullName,
            CalibIntervalMonths = request.CalibIntervalMonths,
            LastCalibDate = request.LastCalibDate,
            CalibType = request.CalibType,
            EquipmentStatus = request.EquipmentStatus,
            Remarks = request.Remarks?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        entity.Id = await _repository.CreateEquipmentAsync(entity);
        var created = await _repository.GetEquipmentByIdAsync(entity.Id);
        return ApiResponse<EquipmentListItemDto>.Created(created!.Equipment);
    }

    public async Task<ApiResponse<EquipmentListItemDto>> UpdateEquipmentAsync(long equipmentId, SaveEquipmentRequest request, string updatedBy)
    {
        var validation = await _equipmentValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<EquipmentListItemDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var entity = await _repository.GetEquipmentEntityByIdAsync(equipmentId);
        if (entity is null)
            return ApiResponse<EquipmentListItemDto>.NotFound("Equipment not found.");

        if (await _repository.EquipmentControlNoExistsAsync(request.ControlNo.Trim(), equipmentId))
            return ApiResponse<EquipmentListItemDto>.Fail($"Control number '{request.ControlNo}' is already in use.");

        var pic = await _repository.GetEmployeeByIdAsync(request.PicId);
        if (pic is null)
            return ApiResponse<EquipmentListItemDto>.Fail("Selected PIC was not found.");

        entity.EquipmentName = request.EquipmentName.Trim();
        entity.ControlNo = request.ControlNo.Trim();
        entity.SerialNo = request.SerialNo?.Trim();
        entity.Brand = request.Brand?.Trim();
        entity.Model = request.Model?.Trim();
        entity.Location = request.Location.Trim();
        entity.SectionId = request.SectionId;
        entity.PicId = request.PicId;
        entity.PicCode = pic.EmployeeCode;
        entity.PicFullName = pic.FullName;
        entity.CalibIntervalMonths = request.CalibIntervalMonths;
        entity.LastCalibDate = request.LastCalibDate;
        entity.CalibType = request.CalibType;
        entity.EquipmentStatus = request.EquipmentStatus;
        entity.Remarks = request.Remarks?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = updatedBy;

        await _repository.UpdateEquipmentAsync(entity);
        var updated = await _repository.GetEquipmentByIdAsync(equipmentId);
        return ApiResponse<EquipmentListItemDto>.Ok(updated!.Equipment, "Updated successfully.");
    }

    public async Task<ApiResponse<BulkActionResultDto>> BulkChangeEquipmentStatusAsync(BulkChangeEquipmentStatusRequest request, string updatedBy)
    {
        var validation = await _bulkStatusValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<BulkActionResultDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        return ApiResponse<BulkActionResultDto>.Ok(await _repository.BulkUpdateEquipmentStatusAsync(request.EquipmentIds.Distinct(), request.EquipmentStatus, request.Remarks, updatedBy));
    }

    public async Task<ApiResponse<BulkActionResultDto>> BulkMoveEquipmentSectionAsync(BulkMoveEquipmentSectionRequest request, string updatedBy)
    {
        var validation = await _bulkSectionValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<BulkActionResultDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        return ApiResponse<BulkActionResultDto>.Ok(await _repository.BulkMoveEquipmentsAsync(request.EquipmentIds.Distinct(), request.SectionId, updatedBy));
    }

    public async Task<ApiResponse<BulkActionResultDto>> BulkDeleteEquipmentsAsync(BulkDeleteEquipmentsRequest request)
    {
        var validation = await _bulkDeleteValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<BulkActionResultDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        return ApiResponse<BulkActionResultDto>.Ok(await _repository.BulkDeleteEquipmentsAsync(request.EquipmentIds.Distinct()));
    }

    public async Task<ApiResponse<PagedResult<ReminderItemDto>>> GetRemindersAsync(ReminderFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetRemindersAsync(filters);
        return ApiResponse<PagedResult<ReminderItemDto>>.Ok(PagedResult<ReminderItemDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<PagedResult<PlanSummaryDto>>> GetPlansAsync(PlanFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetPlansAsync(filters);
        return ApiResponse<PagedResult<PlanSummaryDto>>.Ok(PagedResult<PlanSummaryDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<PlanDetailDto>> GetPlanByIdAsync(long planId)
    {
        var item = await _repository.GetPlanByIdAsync(planId);
        return item is null ? ApiResponse<PlanDetailDto>.NotFound("Plan not found.") : ApiResponse<PlanDetailDto>.Ok(item);
    }

    public async Task<ApiResponse<PlanDetailDto>> CreatePlanAsync(CreatePlanRequest request, string createdBy)
    {
        var validation = await _createPlanValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<PlanDetailDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var planId = await _repository.CreatePlanAsync(request, createdBy);
        var created = await _repository.GetPlanByIdAsync(planId);
        return ApiResponse<PlanDetailDto>.Created(created!);
    }

    public async Task<ApiResponse> UpdatePlanHeaderAsync(long planId, UpdatePlanHeaderRequest request, string updatedBy)
    {
        var updated = await _repository.UpdatePlanHeaderAsync(planId, request.Remarks, updatedBy);
        return updated ? ApiResponse.Ok("Updated successfully.") : ApiResponse.NotFound("Plan not found.");
    }

    public async Task<ApiResponse> SubmitPlanAsync(long planId)
    {
        var updated = await _repository.SubmitPlanAsync(planId);
        return updated ? ApiResponse.Ok("Plan submitted for approval.") : ApiResponse.NotFound("Plan not found or is no longer editable.");
    }

    public async Task<ApiResponse> ActOnPlanApprovalAsync(long planId, long actorEmployeeId, ApprovalActionRequest request, string updatedBy)
    {
        var validation = await _approvalActionValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        return await _repository.ActOnPlanApprovalAsync(planId, actorEmployeeId, request.Action, request.Remarks, updatedBy);
    }

    public Task<ApiResponse<DeleteDocumentResponse>> DeletePlanAsync(long planId, string requestedBy)
        => _repository.DeletePlanAsync(planId, requestedBy);

    public async Task<ApiResponse<ActualDetailDto>> StartActualFromPlanAsync(long planId, CreateActualRequest request, string createdBy)
    {
        var validation = await _createActualValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<ActualDetailDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var response = await _repository.StartActualFromPlanAsync(planId, request, createdBy);
        if (!response.Success)
            return ApiResponse<ActualDetailDto>.Fail(response.Message, response.Errors);

        var created = await _repository.GetActualByIdAsync(response.Data);
        return ApiResponse<ActualDetailDto>.Created(created!);
    }

    public async Task<ApiResponse<PagedResult<ActualSummaryDto>>> GetActualsAsync(ActualFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetActualsAsync(filters);
        return ApiResponse<PagedResult<ActualSummaryDto>>.Ok(PagedResult<ActualSummaryDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<ActualDetailDto>> GetActualByIdAsync(long actualId)
    {
        var item = await _repository.GetActualByIdAsync(actualId);
        return item is null ? ApiResponse<ActualDetailDto>.NotFound("Actual calibration not found.") : ApiResponse<ActualDetailDto>.Ok(item);
    }

    public async Task<ApiResponse<ActualDetailDto>> CreateActualAsync(CreateActualRequest request, string createdBy)
    {
        var validation = await _createActualValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<ActualDetailDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var response = await _repository.CreateActualAsync(request, createdBy);
        if (!response.Success)
            return ApiResponse<ActualDetailDto>.Fail(response.Message, response.Errors);

        var created = await _repository.GetActualByIdAsync(response.Data);
        return ApiResponse<ActualDetailDto>.Created(created!);
    }

    public async Task<ApiResponse> UpdateActualHeaderAsync(long actualId, UpdateActualHeaderRequest request, string updatedBy)
    {
        var updated = await _repository.UpdateActualHeaderAsync(actualId, request.Remarks, updatedBy);
        return updated ? ApiResponse.Ok("Updated successfully.") : ApiResponse.NotFound("Actual calibration not found.");
    }

    public async Task<ApiResponse<ActualWorkerDto>> AddWorkerAsync(long actualId, SaveActualWorkerRequest request, string createdBy)
    {
        var validation = await _workerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<ActualWorkerDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        return await _repository.AddWorkerAsync(actualId, request, createdBy);
    }

    public async Task<ApiResponse> UpdateActualItemAsync(long itemId, UpdateActualItemRequest request, string updatedBy)
        => await _repository.UpdateActualItemAsync(itemId, request, updatedBy);

    public async Task<ApiResponse> UpdateActualItemDetailAsync(long detailId, UpdateActualItemDetailRequest request, string updatedBy)
    {
        var validation = await _actualItemDetailValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        return await _repository.UpdateActualItemDetailAsync(detailId, request, updatedBy);
    }

    public Task<ApiResponse> CompleteActualAsync(long actualId, string completedBy) => _repository.CompleteActualAsync(actualId, completedBy);
    public Task<ApiResponse> SubmitActualAsync(long actualId) => _repository.SubmitActualAsync(actualId);

    public async Task<ApiResponse> ActOnActualApprovalAsync(long actualId, long actorEmployeeId, ApprovalActionRequest request, string updatedBy)
    {
        var validation = await _approvalActionValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        return await _repository.ActOnActualApprovalAsync(actualId, actorEmployeeId, request.Action, request.Remarks, updatedBy);
    }

    public Task<ApiResponse<DeleteDocumentResponse>> DeleteActualAsync(long actualId, string requestedBy)
        => _repository.DeleteActualAsync(actualId, requestedBy);
}
