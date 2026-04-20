using FluentValidation;
using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record SectionDto(long SectionId, string SectionCode, string SectionName, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record PositionDto(long PositionId, string PositionCode, string PositionName, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record LocationDto(long LocationId, string LocationName, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record EmployeeDto(long EmployeeId, string EmployeeCode, string FullName, string? Email, long SectionId, string SectionCode, string SectionName, long? PositionId, string? PositionCode, string? PositionName, long? ManagerId, string? ManagerName, string EmploymentStatus, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public EmployeeDto(int employeeId, string employeeCode, string fullName, string? email, int sectionId, string sectionCode, string sectionName, int? positionId, string? positionCode, string? positionName, int? managerId, string? managerName, string employmentStatus, bool isActive, DateTime createdAt, DateTime? updatedAt)
        : this((long)employeeId, employeeCode, fullName, email, (long)sectionId, sectionCode, sectionName, positionId.HasValue ? (long)positionId.Value : null, positionCode, positionName, managerId.HasValue ? (long)managerId.Value : null, managerName, employmentStatus, isActive, createdAt, updatedAt)
    {
    }
}
public sealed record EmployeeOptionDto(long EmployeeId, string EmployeeCode, string FullName, string? Email, long SectionId, string SectionName)
{
    public EmployeeOptionDto(int employeeId, string employeeCode, string fullName, string? email, int sectionId, string sectionName)
        : this((long)employeeId, employeeCode, fullName, email, (long)sectionId, sectionName)
    {
    }
}
public sealed record ApproverDto(long Id, long EmployeeId, string EmployeeCode, string EmployeeName, long SectionId, string SectionName, string StepNo, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt, string CreatedBy, string? UpdatedBy)
{
    public ApproverDto(int id, int employeeId, string employeeCode, string employeeName, int sectionId, string sectionName, string stepNo, bool isActive, DateTime createdAt, DateTime? updatedAt, string createdBy, string? updatedBy)
        : this((long)id, (long)employeeId, employeeCode, employeeName, (long)sectionId, sectionName, stepNo, isActive, createdAt, updatedAt, createdBy, updatedBy)
    {
    }
}

public sealed class SectionFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class PositionFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class LocationFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class EmployeeFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public long? SectionId { get; set; }
    public long? PositionId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class ApproverFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public string? StepNo { get; set; }
    public long? SectionId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed record SaveSectionRequest(string SectionCode, string SectionName, bool IsActive = true);
public sealed record SavePositionRequest(string PositionCode, string PositionName, bool IsActive = true);
public sealed record SaveLocationRequest(string LocationName, bool IsActive = true);
public sealed record SaveEmployeeRequest(string EmployeeCode, string FullName, string? Email, long SectionId, long? PositionId, long? ManagerId, string EmploymentStatus, bool IsActive = true);
public sealed record SaveApproverRequest(long EmployeeId, string StepNo, bool IsActive = true);

public sealed class SaveSectionRequestValidator : AbstractValidator<SaveSectionRequest>
{
    public SaveSectionRequestValidator()
    {
        RuleFor(x => x.SectionCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SectionName).NotEmpty().MaximumLength(200);
    }
}

public sealed class SavePositionRequestValidator : AbstractValidator<SavePositionRequest>
{
    public SavePositionRequestValidator()
    {
        RuleFor(x => x.PositionCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PositionName).NotEmpty().MaximumLength(200);
    }
}

public sealed class SaveLocationRequestValidator : AbstractValidator<SaveLocationRequest>
{
    public SaveLocationRequestValidator()
    {
        RuleFor(x => x.LocationName).NotEmpty().MaximumLength(200);
    }
}

public sealed class SaveEmployeeRequestValidator : AbstractValidator<SaveEmployeeRequest>
{
    public SaveEmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().Length(6);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.SectionId).GreaterThan(0);
        RuleFor(x => x.PositionId).GreaterThan(0).When(x => x.PositionId.HasValue);
        RuleFor(x => x.ManagerId).GreaterThan(0).When(x => x.ManagerId.HasValue);
        RuleFor(x => x.EmploymentStatus).NotEmpty().MaximumLength(50);
    }
}

public sealed class SaveApproverRequestValidator : AbstractValidator<SaveApproverRequest>
{
    public SaveApproverRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.StepNo).Must(x => x is "1" or "2" or "3");
    }
}

public sealed class EquipmentFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public long? SectionId { get; set; }
    public string? Location { get; set; }
    public string? EquipmentStatus { get; set; }
    public string? CalibType { get; set; }
    public string? DueCategory { get; set; }
}

public sealed class ReminderFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public long? SectionId { get; set; }
    public string? CalibType { get; set; }
    public string? DueCategory { get; set; }
    public string? Location { get; set; }
}

public sealed class PlanFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public string? CalibType { get; set; }
    public long? SectionId { get; set; }
    public string? Status { get; set; }
}

public sealed class ActualFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public string? CalibType { get; set; }
    public long? SectionId { get; set; }
    public string? CompletionStatus { get; set; }
}

public sealed record EquipmentSummaryCardsDto(
    int TotalEquipments,
    int Active,
    int OutOfService,
    int Scrapped,
    int InternalCalibration,
    int ExternalCalibration,
    int DueThisMonth,
    int Overdue);

public sealed record EquipmentGroupSummaryDto(
    string EquipmentName,
    int TotalQuantity,
    int ActiveCount,
    int OutOfServiceCount,
    int ScrappedCount,
    int DueThisMonthCount,
    int OverdueCount);

public sealed record EquipmentListItemDto(
    long Id,
    string EquipmentName,
    string ControlNo,
    string? SerialNo,
    string? Brand,
    string? Model,
    string Location,
    long SectionId,
    string SectionCode,
    string SectionName,
    long PicId,
    string PicCode,
    string PicFullName,
    int CalibIntervalMonths,
    DateOnly LastCalibDate,
    DateOnly NextCalibDate,
    string CalibType,
    string EquipmentStatus,
    string? Remarks)
{
    public EquipmentListItemDto(
        int id,
        string equipmentName,
        string controlNo,
        string? serialNo,
        string? brand,
        string? model,
        string location,
        int sectionId,
        string sectionCode,
        string sectionName,
        int picId,
        string picCode,
        string picFullName,
        int calibIntervalMonths,
        DateTime lastCalibDate,
        DateTime nextCalibDate,
        string calibType,
        string equipmentStatus,
        string? remarks)
        : this(
            (long)id,
            equipmentName,
            controlNo,
            serialNo,
            brand,
            model,
            location,
            (long)sectionId,
            sectionCode,
            sectionName,
            (long)picId,
            picCode,
            picFullName,
            calibIntervalMonths,
            DateOnly.FromDateTime(lastCalibDate),
            DateOnly.FromDateTime(nextCalibDate),
            calibType,
            equipmentStatus,
            remarks)
    {
    }
}

public sealed record CalibrationHistoryEntryDto(
    long HeaderId,
    string CalibNo,
    string CalibPhase,
    string CalibType,
    int CalibMonth,
    int CalibYear,
    string DocumentStatus,
    string? CalibResult,
    bool OverdueFlag,
    string? CertificateNo,
    string? Remarks,
    DateTime CreatedAt)
{
    public CalibrationHistoryEntryDto(int headerId, string calibNo, string calibPhase, string calibType, int calibMonth, int calibYear, string documentStatus, string? calibResult, bool overdueFlag, string? certificateNo, string? remarks, DateTime createdAt)
        : this((long)headerId, calibNo, calibPhase, calibType, calibMonth, calibYear, documentStatus, calibResult, overdueFlag, certificateNo, remarks, createdAt)
    {
    }
}

public sealed record EquipmentDetailDto(
    EquipmentListItemDto Equipment,
    IEnumerable<CalibrationHistoryEntryDto> History);

public sealed record ReminderItemDto(
    long EquipmentId,
    string EquipmentName,
    string ControlNo,
    string SectionName,
    string PicCode,
    string PicFullName,
    string Location,
    DateOnly LastCalibDate,
    DateOnly NextCalibDate,
    string CalibType,
    string DueCategory,
    bool IsOverdue)
{
    public ReminderItemDto(int equipmentId, string equipmentName, string controlNo, string sectionName, string picCode, string picFullName, string location, DateTime lastCalibDate, DateTime nextCalibDate, string calibType, string dueCategory, bool isOverdue)
        : this((long)equipmentId, equipmentName, controlNo, sectionName, picCode, picFullName, location, DateOnly.FromDateTime(lastCalibDate), DateOnly.FromDateTime(nextCalibDate), calibType, dueCategory, isOverdue)
    {
    }
}

public sealed record BulkActionItemResultDto(long EquipmentId, string ControlNo, bool Success, string Message);
public sealed record BulkActionResultDto(int RequestedCount, int SuccessCount, int FailedCount, IEnumerable<BulkActionItemResultDto> Items);

public sealed record SaveEquipmentRequest(
    string EquipmentName,
    string ControlNo,
    string? SerialNo,
    string? Brand,
    string? Model,
    string Location,
    long SectionId,
    long PicId,
    int CalibIntervalMonths,
    DateOnly LastCalibDate,
    string CalibType,
    string EquipmentStatus,
    string? Remarks);

public sealed record BulkChangeEquipmentStatusRequest(IEnumerable<long> EquipmentIds, string EquipmentStatus, string? Remarks);
public sealed record BulkMoveEquipmentSectionRequest(IEnumerable<long> EquipmentIds, long SectionId);
public sealed record BulkDeleteEquipmentsRequest(IEnumerable<long> EquipmentIds);

public sealed class SaveEquipmentRequestValidator : AbstractValidator<SaveEquipmentRequest>
{
    public SaveEquipmentRequestValidator()
    {
        RuleFor(x => x.EquipmentName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ControlNo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SectionId).GreaterThan(0);
        RuleFor(x => x.PicId).GreaterThan(0);
        RuleFor(x => x.CalibIntervalMonths).GreaterThan(0);
        RuleFor(x => x.CalibType).Must(x => x is "I" or "E");
        RuleFor(x => x.EquipmentStatus).Must(x => x is "A" or "O" or "S");
    }
}

public sealed class BulkChangeEquipmentStatusRequestValidator : AbstractValidator<BulkChangeEquipmentStatusRequest>
{
    public BulkChangeEquipmentStatusRequestValidator()
    {
        RuleFor(x => x.EquipmentIds).NotEmpty();
        RuleFor(x => x.EquipmentStatus).Must(x => x is "A" or "O" or "S");
    }
}

public sealed class BulkMoveEquipmentSectionRequestValidator : AbstractValidator<BulkMoveEquipmentSectionRequest>
{
    public BulkMoveEquipmentSectionRequestValidator()
    {
        RuleFor(x => x.EquipmentIds).NotEmpty();
        RuleFor(x => x.SectionId).GreaterThan(0);
    }
}

public sealed class BulkDeleteEquipmentsRequestValidator : AbstractValidator<BulkDeleteEquipmentsRequest>
{
    public BulkDeleteEquipmentsRequestValidator()
    {
        RuleFor(x => x.EquipmentIds).NotEmpty();
    }
}

public sealed record DashboardCountItemDto(string Label, int Total);
public sealed record DashboardGroupedItemDto(string Label, string Group, int Total);
public sealed record DashboardMonthlyPlanActualDto(int Month, int PlannedTotal, int ActualTotal);
public sealed record DashboardReminderSummaryDto(string DueCategory, int Total);
public sealed record DashboardOverviewDto(
    IEnumerable<DashboardCountItemDto> EquipmentsBySection,
    IEnumerable<DashboardCountItemDto> EquipmentsByCalibrationType,
    IEnumerable<DashboardCountItemDto> EquipmentsByName,
    IEnumerable<DashboardGroupedItemDto> CalibrationOverviewByPeriod,
    IEnumerable<DashboardMonthlyPlanActualDto> CalibrationPerformanceYearly,
    IEnumerable<DashboardCountItemDto> EquipmentStatusOverview,
    IEnumerable<DashboardGroupedItemDto> OutOfServiceBySection,
    IEnumerable<DashboardGroupedItemDto> StatusBySection,
    IEnumerable<DashboardReminderSummaryDto> ReminderSummary,
    IEnumerable<ReminderItemDto> ReminderItems,
    IEnumerable<ReminderItemDto> YearlySchedule);

public sealed record PlanSummaryDto(
    long Id,
    long HeaderId,
    string CalibNo,
    int CalibMonth,
    int CalibYear,
    string CalibType,
    string Status,
    string? Preparer,
    string? Checker,
    string? Approver,
    string CreatedBy,
    DateTime CreatedAt,
    bool HasActual)
{
    public PlanSummaryDto(int id, int headerId, string calibNo, int calibMonth, int calibYear, string calibType, string status, string? preparer, string? checker, string? approver, string createdBy, DateTime createdAt, bool hasActual)
        : this((long)id, (long)headerId, calibNo, calibMonth, calibYear, calibType, status, preparer, checker, approver, createdBy, createdAt, hasActual)
    {
    }
}

public sealed record ApprovalStepDto(string StepNo, string StepName, long? EmployeeId, string? EmployeeCode, string? EmployeeFullName, string Action, string? Remarks, DateTime? ActionedAt)
{
    public ApprovalStepDto(string stepNo, string stepName, int? employeeId, string? employeeCode, string? employeeFullName, string action, string? remarks, DateTime? actionedAt)
        : this(stepNo, stepName, employeeId.HasValue ? (long)employeeId.Value : null, employeeCode, employeeFullName, action, remarks, actionedAt)
    {
    }
}
public sealed record PlanItemDto(long Id, string EquipmentName, int ItemCount, int ItemCompleted, string? StdUsed, string? Remarks, IEnumerable<PlanItemDetailDto> Details);
public sealed record PlanItemDetailDto(long Id, long EquipmentId, string ControlNo, string SectionName, string PicFullName, DateOnly NextCalibDate, string? CalibResult, bool OverdueFlag, string? CertificateNo, string? Remarks)
{
    public PlanItemDetailDto(int id, int equipmentId, string controlNo, string sectionName, string picFullName, DateTime nextCalibDate, string? calibResult, bool overdueFlag, string? certificateNo, string? remarks)
        : this((long)id, (long)equipmentId, controlNo, sectionName, picFullName, DateOnly.FromDateTime(nextCalibDate), calibResult, overdueFlag, certificateNo, remarks)
    {
    }
}
public sealed record PlanDetailDto(
    long Id,
    long HeaderId,
    string CalibNo,
    int CalibMonth,
    int CalibYear,
    string CalibType,
    string Status,
    string? Remarks,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<ApprovalStepDto> Approvals,
    IEnumerable<PlanItemDto> Items,
    bool HasActual);

public sealed record CreatePlanRequest(int CalibMonth, int CalibYear, string CalibType, string? Remarks, long PreparerEmployeeId, long CheckerEmployeeId, long ApproverEmployeeId, long[] EquipmentIds);
public sealed record UpdatePlanHeaderRequest(string? Remarks);
public sealed record ApprovalActionRequest(string Action, string? Remarks);
public sealed record DeleteDocumentResponse(long Id, string DocumentNo, string Message);

public sealed class CreatePlanRequestValidator : AbstractValidator<CreatePlanRequest>
{
    public CreatePlanRequestValidator()
    {
        RuleFor(x => x.CalibMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.CalibYear).GreaterThanOrEqualTo(2024);
        RuleFor(x => x.CalibType).Must(x => x is "I" or "E");
        RuleFor(x => x.PreparerEmployeeId).GreaterThan(0);
        RuleFor(x => x.CheckerEmployeeId).GreaterThan(0);
        RuleFor(x => x.ApproverEmployeeId).GreaterThan(0);
        RuleFor(x => x.EquipmentIds).NotEmpty();
    }
}

public sealed class ApprovalActionRequestValidator : AbstractValidator<ApprovalActionRequest>
{
    public ApprovalActionRequestValidator()
    {
        RuleFor(x => x.Action).Must(x => x is "S" or "C");
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public sealed record ActualSummaryDto(
    long Id,
    long PlanId,
    long HeaderId,
    string CalibNo,
    string? LinkedPlanNo,
    int CalibMonth,
    int CalibYear,
    string CalibType,
    string CompletionStatus,
    string? Preparer,
    string? Checker,
    string? Approver,
    string CreatedBy,
    DateTime CreatedAt)
{
    public ActualSummaryDto(int id, int planId, int headerId, string calibNo, string? linkedPlanNo, int calibMonth, int calibYear, string calibType, string completionStatus, string? preparer, string? checker, string? approver, string createdBy, DateTime createdAt)
        : this((long)id, (long)planId, (long)headerId, calibNo, linkedPlanNo, calibMonth, calibYear, calibType, completionStatus, preparer, checker, approver, createdBy, createdAt)
    {
    }
}

public sealed record ActualWorkerDto(long Id, long? EmployeeId, string? EmployeeCode, string? EmployeeFullName, string? ExternalPartyName, bool IsPic)
{
    public ActualWorkerDto(int id, int? employeeId, string? employeeCode, string? employeeFullName, string? externalPartyName, bool isPic)
        : this((long)id, employeeId.HasValue ? (long)employeeId.Value : null, employeeCode, employeeFullName, externalPartyName, isPic)
    {
    }
}
public sealed record ActualDetailDto(
    long Id,
    long PlanId,
    long HeaderId,
    string CalibNo,
    string? LinkedPlanNo,
    int CalibMonth,
    int CalibYear,
    string CalibType,
    string CompletionStatus,
    string? Remarks,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int PlannedItemCount,
    int CompletedItemCount,
    IEnumerable<ApprovalStepDto> Approvals,
    IEnumerable<ActualWorkerDto> Workers,
    IEnumerable<PlanItemDto> Items);

public sealed record CreateActualRequest(long PlanId, long PreparerEmployeeId, long CheckerEmployeeId, long ApproverEmployeeId, string? Remarks);
public sealed record UpdateActualHeaderRequest(string? Remarks);
public sealed record SaveActualWorkerRequest(long? EmployeeId, string? ExternalPartyName, bool IsPic);
public sealed record UpdateActualItemRequest(string? StdUsed, string? Remarks);
public sealed record UpdateActualItemDetailRequest(string? CalibResult, bool OverdueFlag, string? CertificateNo, string? Remarks);

public sealed class CreateActualRequestValidator : AbstractValidator<CreateActualRequest>
{
    public CreateActualRequestValidator()
    {
        RuleFor(x => x.PlanId).GreaterThan(0);
        RuleFor(x => x.PreparerEmployeeId).GreaterThan(0);
        RuleFor(x => x.CheckerEmployeeId).GreaterThan(0);
        RuleFor(x => x.ApproverEmployeeId).GreaterThan(0);
    }
}

public sealed class SaveActualWorkerRequestValidator : AbstractValidator<SaveActualWorkerRequest>
{
    public SaveActualWorkerRequestValidator()
    {
        RuleFor(x => x).Must(x => x.EmployeeId.HasValue || !string.IsNullOrWhiteSpace(x.ExternalPartyName))
            .WithMessage("Either an employee or an external party name is required.");
    }
}

public sealed class UpdateActualItemDetailRequestValidator : AbstractValidator<UpdateActualItemDetailRequest>
{
    public UpdateActualItemDetailRequestValidator()
    {
        RuleFor(x => x.CalibResult).Must(x => x is null or "O" or "N");
        RuleFor(x => x.CertificateNo).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.CertificateNo));
    }
}
