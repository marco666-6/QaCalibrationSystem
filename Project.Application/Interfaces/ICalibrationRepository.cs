using Project.Application.Common;
using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;

public interface ICalibrationRepository
{
    Task<DashboardOverviewDto> GetDashboardOverviewAsync(int? year, long? sectionId, string? status);

    Task<(IEnumerable<ApproverDto> Items, int TotalCount)> GetApproversAsync(ApproverFilterParams filters);
    Task<ApproverDto?> GetApproverByIdAsync(long approverId);
    Task<bool> ApproverAssignmentExistsAsync(long employeeId, string stepNo, long? excludeId = null);
    Task<long> CreateApproverAsync(CalibrationApprover approver);
    Task<bool> UpdateApproverAsync(CalibrationApprover approver);
    Task<Employee?> GetEmployeeByIdAsync(long employeeId);

    Task<EquipmentSummaryCardsDto> GetEquipmentSummaryCardsAsync();
    Task<IEnumerable<EquipmentGroupSummaryDto>> GetEquipmentGroupSummaryAsync(string? search, long? sectionId, string? dueCategory);
    Task<(IEnumerable<EquipmentListItemDto> Items, int TotalCount)> GetEquipmentsAsync(EquipmentFilterParams filters);
    Task<EquipmentDetailDto?> GetEquipmentByIdAsync(long equipmentId);
    Task<bool> EquipmentControlNoExistsAsync(string controlNo, long? excludeId = null);
    Task<long> CreateEquipmentAsync(CalibrationEquipment equipment);
    Task<bool> UpdateEquipmentAsync(CalibrationEquipment equipment);
    Task<CalibrationEquipment?> GetEquipmentEntityByIdAsync(long equipmentId);
    Task<BulkActionResultDto> BulkUpdateEquipmentStatusAsync(IEnumerable<long> equipmentIds, string status, string? remarks, string updatedBy);
    Task<BulkActionResultDto> BulkMoveEquipmentsAsync(IEnumerable<long> equipmentIds, long sectionId, string updatedBy);
    Task<BulkActionResultDto> BulkDeleteEquipmentsAsync(IEnumerable<long> equipmentIds);
    Task<(IEnumerable<ReminderItemDto> Items, int TotalCount)> GetRemindersAsync(ReminderFilterParams filters);

    Task<(IEnumerable<PlanSummaryDto> Items, int TotalCount)> GetPlansAsync(PlanFilterParams filters);
    Task<PlanDetailDto?> GetPlanByIdAsync(long planId);
    Task<long> CreatePlanAsync(CreatePlanRequest request, string createdBy);
    Task<bool> UpdatePlanHeaderAsync(long planId, string? remarks, string updatedBy);
    Task<bool> SubmitPlanAsync(long planId);
    Task<ApiResponse> ActOnPlanApprovalAsync(long planId, long actorEmployeeId, string action, string? remarks, string updatedBy);
    Task<ApiResponse<DeleteDocumentResponse>> DeletePlanAsync(long planId, string requestedBy);
    Task<ApiResponse<long>> StartActualFromPlanAsync(long planId, CreateActualRequest request, string createdBy);

    Task<(IEnumerable<ActualSummaryDto> Items, int TotalCount)> GetActualsAsync(ActualFilterParams filters);
    Task<ActualDetailDto?> GetActualByIdAsync(long actualId);
    Task<ApiResponse<long>> CreateActualAsync(CreateActualRequest request, string createdBy);
    Task<bool> UpdateActualHeaderAsync(long actualId, string? remarks, string updatedBy);
    Task<ApiResponse<ActualWorkerDto>> AddWorkerAsync(long actualId, SaveActualWorkerRequest request, string createdBy);
    Task<ApiResponse> UpdateActualItemAsync(long itemId, UpdateActualItemRequest request, string updatedBy);
    Task<ApiResponse> UpdateActualItemDetailAsync(long detailId, UpdateActualItemDetailRequest request, string updatedBy);
    Task<ApiResponse> CompleteActualAsync(long actualId, string completedBy);
    Task<ApiResponse> SubmitActualAsync(long actualId);
    Task<ApiResponse> ActOnActualApprovalAsync(long actualId, long actorEmployeeId, string action, string? remarks, string updatedBy);
    Task<ApiResponse<DeleteDocumentResponse>> DeleteActualAsync(long actualId, string requestedBy);
}
