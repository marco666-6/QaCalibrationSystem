using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface ICalibrationService
{
    Task<ApiResponse<DashboardOverviewDto>> GetDashboardOverviewAsync(int? year, long? sectionId, string? status);

    Task<ApiResponse<PagedResult<ApproverDto>>> GetApproversAsync(ApproverFilterParams filters);
    Task<ApiResponse<ApproverDto>> CreateApproverAsync(SaveApproverRequest request, string createdBy);
    Task<ApiResponse<ApproverDto>> UpdateApproverAsync(long approverId, SaveApproverRequest request, string updatedBy);

    Task<ApiResponse<EquipmentSummaryCardsDto>> GetEquipmentSummaryCardsAsync();
    Task<ApiResponse<IEnumerable<EquipmentGroupSummaryDto>>> GetEquipmentGroupSummaryAsync(string? search, long? sectionId, string? dueCategory);
    Task<ApiResponse<PagedResult<EquipmentListItemDto>>> GetEquipmentsAsync(EquipmentFilterParams filters);
    Task<ApiResponse<EquipmentDetailDto>> GetEquipmentByIdAsync(long equipmentId);
    Task<ApiResponse<EquipmentListItemDto>> CreateEquipmentAsync(SaveEquipmentRequest request, string createdBy);
    Task<ApiResponse<EquipmentListItemDto>> UpdateEquipmentAsync(long equipmentId, SaveEquipmentRequest request, string updatedBy);
    Task<ApiResponse<BulkActionResultDto>> BulkChangeEquipmentStatusAsync(BulkChangeEquipmentStatusRequest request, string updatedBy);
    Task<ApiResponse<BulkActionResultDto>> BulkMoveEquipmentSectionAsync(BulkMoveEquipmentSectionRequest request, string updatedBy);
    Task<ApiResponse<BulkActionResultDto>> BulkDeleteEquipmentsAsync(BulkDeleteEquipmentsRequest request);
    Task<ApiResponse<PagedResult<ReminderItemDto>>> GetRemindersAsync(ReminderFilterParams filters);

    Task<ApiResponse<PagedResult<PlanSummaryDto>>> GetPlansAsync(PlanFilterParams filters);
    Task<ApiResponse<PlanDetailDto>> GetPlanByIdAsync(long planId);
    Task<ApiResponse<PlanDetailDto>> CreatePlanAsync(CreatePlanRequest request, string createdBy);
    Task<ApiResponse> UpdatePlanHeaderAsync(long planId, UpdatePlanHeaderRequest request, string updatedBy);
    Task<ApiResponse> SubmitPlanAsync(long planId);
    Task<ApiResponse> ActOnPlanApprovalAsync(long planId, long actorEmployeeId, ApprovalActionRequest request, string updatedBy);
    Task<ApiResponse<DeleteDocumentResponse>> DeletePlanAsync(long planId, string requestedBy);
    Task<ApiResponse<ActualDetailDto>> StartActualFromPlanAsync(long planId, CreateActualRequest request, string createdBy);

    Task<ApiResponse<PagedResult<ActualSummaryDto>>> GetActualsAsync(ActualFilterParams filters);
    Task<ApiResponse<ActualDetailDto>> GetActualByIdAsync(long actualId);
    Task<ApiResponse<ActualDetailDto>> CreateActualAsync(CreateActualRequest request, string createdBy);
    Task<ApiResponse> UpdateActualHeaderAsync(long actualId, UpdateActualHeaderRequest request, string updatedBy);
    Task<ApiResponse<ActualWorkerDto>> AddWorkerAsync(long actualId, SaveActualWorkerRequest request, string createdBy);
    Task<ApiResponse> UpdateActualItemAsync(long itemId, UpdateActualItemRequest request, string updatedBy);
    Task<ApiResponse> UpdateActualItemDetailAsync(long detailId, UpdateActualItemDetailRequest request, string updatedBy);
    Task<ApiResponse> CompleteActualAsync(long actualId, string completedBy);
    Task<ApiResponse> SubmitActualAsync(long actualId);
    Task<ApiResponse> ActOnActualApprovalAsync(long actualId, long actorEmployeeId, ApprovalActionRequest request, string updatedBy);
    Task<ApiResponse<DeleteDocumentResponse>> DeleteActualAsync(long actualId, string requestedBy);
}
