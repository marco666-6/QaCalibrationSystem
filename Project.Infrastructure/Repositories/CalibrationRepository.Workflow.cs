using System.Data;
using Dapper;
using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Infrastructure.Repositories;

public sealed partial class CalibrationRepository
{
    public async Task<(IEnumerable<PlanSummaryDto> Items, int TotalCount)> GetPlansAsync(PlanFilterParams filters)
    {
        var where = new List<string> { "h.calib_phase = 'P'" };
        var parameters = new DynamicParameters(new { filters.Offset, filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(h.calib_no LIKE @Search OR h.remarks LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (filters.Month.HasValue)
        {
            where.Add("h.calib_month = @Month");
            parameters.Add("Month", filters.Month.Value);
        }
        if (filters.Year.HasValue)
        {
            where.Add("h.calib_year = @Year");
            parameters.Add("Year", filters.Year.Value);
        }
        if (!string.IsNullOrWhiteSpace(filters.CalibType))
        {
            where.Add("h.calib_type = @CalibType");
            parameters.Add("CalibType", filters.CalibType.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            where.Add("p.calib_status = @Status");
            parameters.Add("Status", filters.Status.Trim());
        }
        if (filters.SectionId.HasValue)
        {
            where.Add("EXISTS (SELECT 1 FROM dbo.qa_calib_item_details d INNER JOIN dbo.qa_calib_items i ON i.id = d.item_id INNER JOIN dbo.qa_calib_equipments e ON e.id = d.equipment_id WHERE i.header_id = h.id AND e.section_id = @SectionId)");
            parameters.Add("SectionId", filters.SectionId.Value);
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM dbo.qa_calib_main_headers h
            INNER JOIN dbo.qa_calib_plans p ON p.header_id = h.id
            WHERE {whereClause}
            """, parameters);
        var items = await connection.QueryAsync<PlanSummaryDto>($"""
            SELECT
                p.id,
                h.id AS header_id,
                h.calib_no,
                h.calib_month,
                h.calib_year,
                h.calib_type,
                p.calib_status AS status,
                MAX(CASE WHEN ap.step_no = '1' THEN ap.employee_full_name END) AS preparer,
                MAX(CASE WHEN ap.step_no = '2' THEN ap.employee_full_name END) AS checker,
                MAX(CASE WHEN ap.step_no = '3' THEN ap.employee_full_name END) AS approver,
                h.created_by,
                h.created_at,
                CASE WHEN EXISTS (SELECT 1 FROM dbo.qa_calib_actuals a WHERE a.plan_id = p.id) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS has_actual
            FROM dbo.qa_calib_main_headers h
            INNER JOIN dbo.qa_calib_plans p ON p.header_id = h.id
            LEFT JOIN dbo.qa_calib_approvals ap ON ap.header_id = h.id
            WHERE {whereClause}
            GROUP BY p.id, h.id, h.calib_no, h.calib_month, h.calib_year, h.calib_type, p.calib_status, h.created_by, h.created_at
            ORDER BY h.calib_year DESC, h.calib_month DESC, p.id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public async Task<PlanDetailDto?> GetPlanByIdAsync(long planId)
    {
        using var connection = CreateConnection();
        var header = await connection.QuerySingleOrDefaultAsync<PlanDetailHeaderRow>(
            """
            SELECT
                p.id,
                h.id AS header_id,
                h.calib_no,
                h.calib_month,
                h.calib_year,
                h.calib_type,
                p.calib_status AS status,
                h.remarks,
                h.created_by,
                h.created_at,
                h.updated_at
            FROM dbo.qa_calib_plans p
            INNER JOIN dbo.qa_calib_main_headers h ON h.id = p.header_id
            WHERE p.id = @PlanId
            """,
            new { PlanId = planId });

        if (header is null)
            return null;

        return await BuildPlanDetailAsync(connection, header.Id, header.HeaderId, header.CalibNo, header.CalibMonth, header.CalibYear, header.CalibType, header.Status, header.Remarks, header.CreatedBy, header.CreatedAt, header.UpdatedAt);
    }

    public async Task<long> CreatePlanAsync(CreatePlanRequest request, string createdBy)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var headerId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO dbo.qa_calib_main_headers (calib_no, calib_phase, calib_type, calib_month, calib_year, remarks, created_at, updated_at, created_by, updated_by)
            VALUES (@CalibNo, 'P', @CalibType, @CalibMonth, @CalibYear, @Remarks, sysutcdatetime(), NULL, @CreatedBy, NULL);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                CalibNo = await GenerateCalibNoAsync(connection, transaction, "P", request.CalibYear, request.CalibMonth),
                request.CalibType,
                request.CalibMonth,
                request.CalibYear,
                request.Remarks,
                CreatedBy = createdBy
            },
            transaction);

        var planId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO dbo.qa_calib_plans (header_id, calib_status)
            VALUES (@HeaderId, 'D');
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new { HeaderId = headerId },
            transaction);

        await InsertApprovalRowsAsync(connection, transaction, headerId, request.PreparerEmployeeId, request.CheckerEmployeeId, request.ApproverEmployeeId, createdBy);
        await InsertPlanItemsAsync(connection, transaction, headerId, request.EquipmentIds, createdBy);

        transaction.Commit();
        return planId;
    }

    public async Task<bool> UpdatePlanHeaderAsync(long planId, string? remarks, string updatedBy)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteAsync(
            """
            UPDATE h
            SET h.remarks = @Remarks,
                h.updated_at = sysutcdatetime(),
                h.updated_by = @UpdatedBy
            FROM dbo.qa_calib_main_headers h
            INNER JOIN dbo.qa_calib_plans p ON p.header_id = h.id
            WHERE p.id = @PlanId AND p.calib_status = 'D'
            """,
            new { PlanId = planId, Remarks = remarks, UpdatedBy = updatedBy }) > 0;
    }

    public async Task<bool> SubmitPlanAsync(long planId)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteAsync("UPDATE dbo.qa_calib_plans SET calib_status = 'S' WHERE id = @PlanId AND calib_status = 'D'", new { PlanId = planId }) > 0;
    }

    public async Task<ApiResponse> ActOnPlanApprovalAsync(long planId, long actorEmployeeId, string action, string? remarks, string updatedBy)
    {
        using var connection = CreateConnection();
        var plan = await connection.QuerySingleOrDefaultAsync<(long HeaderId, string Status)>(
            "SELECT header_id AS HeaderId, calib_status AS Status FROM dbo.qa_calib_plans WHERE id = @PlanId",
            new { PlanId = planId });

        if (plan == default)
            return ApiResponse.NotFound("Plan not found.");

        if (plan.Status != "S")
            return ApiResponse.Fail("Plan is not in approval status.");

        var approvals = (await connection.QueryAsync<ApprovalStepDto>(
            """
            SELECT step_no, CASE step_no WHEN '1' THEN 'Prepared' WHEN '2' THEN 'Checked' ELSE 'Approved' END AS step_name,
                   employee_id, employee_code, employee_full_name, action, remarks, actioned_at
            FROM dbo.qa_calib_approvals
            WHERE header_id = @HeaderId
            ORDER BY step_no
            """,
            new { plan.HeaderId })).ToList();

        var current = approvals.FirstOrDefault(x => x.EmployeeId == actorEmployeeId);
        if (current is null)
            return ApiResponse.Fail("You are not assigned to this plan approval chain.");

        var previousPending = approvals.Where(x => string.CompareOrdinal(x.StepNo, current.StepNo) < 0).Any(x => x.Action != "S" || !x.ActionedAt.HasValue);
        if (previousPending)
            return ApiResponse.Fail("Previous approval steps must be completed first.");

        if (action == "C")
        {
            var canCancel = CanCancelApproval(current.StepNo, current.Action, current.ActionedAt, approvals);
            if (!canCancel.Success)
                return ApiResponse.Fail(canCancel.Message);
        }

        await connection.ExecuteAsync(
            """
            UPDATE dbo.qa_calib_approvals
            SET action = @Action,
                remarks = @Remarks,
                actioned_at = sysutcdatetime(),
                updated_at = sysutcdatetime(),
                updated_by = @UpdatedBy
            WHERE header_id = @HeaderId AND employee_id = @EmployeeId
            """,
            new { HeaderId = plan.HeaderId, EmployeeId = actorEmployeeId, Action = action, Remarks = remarks, UpdatedBy = updatedBy });

        var refreshed = (await connection.QueryAsync<ApprovalStepDto>(
            "SELECT step_no, CASE step_no WHEN '1' THEN 'Prepared' WHEN '2' THEN 'Checked' ELSE 'Approved' END AS step_name, employee_id, employee_code, employee_full_name, action, remarks, actioned_at FROM dbo.qa_calib_approvals WHERE header_id = @HeaderId ORDER BY step_no",
            new { plan.HeaderId })).ToList();

        if (action == "C")
        {
            await connection.ExecuteAsync("UPDATE dbo.qa_calib_plans SET calib_status = 'S', locked_at = NULL, locked_by = NULL WHERE id = @PlanId", new { PlanId = planId });
            return ApiResponse.Ok("Plan approval was cancelled.");
        }

        if (refreshed.All(x => x.Action == "S" && x.ActionedAt.HasValue))
        {
            await connection.ExecuteAsync("UPDATE dbo.qa_calib_plans SET calib_status = 'L', locked_at = sysutcdatetime(), locked_by = @UpdatedBy WHERE id = @PlanId", new { PlanId = planId, UpdatedBy = updatedBy });
            return ApiResponse.Ok("Plan fully approved and locked.");
        }

        return ApiResponse.Ok("Approval action saved.");
    }

    public Task<ApiResponse<long>> StartActualFromPlanAsync(long planId, CreateActualRequest request, string createdBy)
        => CreateActualFromPlanInternalAsync(planId, request, createdBy);

    public async Task<(IEnumerable<ActualSummaryDto> Items, int TotalCount)> GetActualsAsync(ActualFilterParams filters)
    {
        var where = new List<string> { "h.calib_phase = 'A'" };
        var parameters = new DynamicParameters(new { filters.Offset, filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(h.calib_no LIKE @Search OR h.remarks LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (filters.Month.HasValue)
        {
            where.Add("h.calib_month = @Month");
            parameters.Add("Month", filters.Month.Value);
        }
        if (filters.Year.HasValue)
        {
            where.Add("h.calib_year = @Year");
            parameters.Add("Year", filters.Year.Value);
        }
        if (!string.IsNullOrWhiteSpace(filters.CalibType))
        {
            where.Add("h.calib_type = @CalibType");
            parameters.Add("CalibType", filters.CalibType.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filters.CompletionStatus))
        {
            where.Add("a.calib_status = @CompletionStatus");
            parameters.Add("CompletionStatus", filters.CompletionStatus.Trim());
        }
        if (filters.SectionId.HasValue)
        {
            where.Add("EXISTS (SELECT 1 FROM dbo.qa_calib_item_details d INNER JOIN dbo.qa_calib_items i ON i.id = d.item_id INNER JOIN dbo.qa_calib_equipments e ON e.id = d.equipment_id WHERE i.header_id = h.id AND e.section_id = @SectionId)");
            parameters.Add("SectionId", filters.SectionId.Value);
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM dbo.qa_calib_main_headers h
            INNER JOIN dbo.qa_calib_actuals a ON a.header_id = h.id
            WHERE {whereClause}
            """, parameters);
        var items = await connection.QueryAsync<ActualSummaryDto>($"""
            SELECT
                a.id,
                a.plan_id,
                h.id AS header_id,
                h.calib_no,
                ph.calib_no AS linked_plan_no,
                h.calib_month,
                h.calib_year,
                h.calib_type,
                a.calib_status AS completion_status,
                MAX(CASE WHEN ap.step_no = '1' THEN ap.employee_full_name END) AS preparer,
                MAX(CASE WHEN ap.step_no = '2' THEN ap.employee_full_name END) AS checker,
                MAX(CASE WHEN ap.step_no = '3' THEN ap.employee_full_name END) AS approver,
                h.created_by,
                h.created_at
            FROM dbo.qa_calib_main_headers h
            INNER JOIN dbo.qa_calib_actuals a ON a.header_id = h.id
            INNER JOIN dbo.qa_calib_plans p ON p.id = a.plan_id
            INNER JOIN dbo.qa_calib_main_headers ph ON ph.id = p.header_id
            LEFT JOIN dbo.qa_calib_approvals ap ON ap.header_id = h.id
            WHERE {whereClause}
            GROUP BY a.id, a.plan_id, h.id, h.calib_no, ph.calib_no, h.calib_month, h.calib_year, h.calib_type, a.calib_status, h.created_by, h.created_at
            ORDER BY h.calib_year DESC, h.calib_month DESC, a.id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public async Task<ActualDetailDto?> GetActualByIdAsync(long actualId)
    {
        using var connection = CreateConnection();
        var header = await connection.QuerySingleOrDefaultAsync<ActualDetailHeaderRow>(
            """
            SELECT
                a.id,
                a.plan_id,
                h.id AS header_id,
                h.calib_no,
                ph.calib_no AS linked_plan_no,
                h.calib_month,
                h.calib_year,
                h.calib_type,
                a.calib_status AS completion_status,
                h.remarks,
                h.created_by,
                h.created_at,
                h.updated_at
            FROM dbo.qa_calib_actuals a
            INNER JOIN dbo.qa_calib_main_headers h ON h.id = a.header_id
            INNER JOIN dbo.qa_calib_plans p ON p.id = a.plan_id
            INNER JOIN dbo.qa_calib_main_headers ph ON ph.id = p.header_id
            WHERE a.id = @ActualId
            """,
            new { ActualId = actualId });

        if (header is null)
            return null;

        var approvals = await GetApprovalsAsync(connection, header.HeaderId);
        var workers = await connection.QueryAsync<ActualWorkerDto>(
            """
            SELECT id, employee_id, employee_code, employee_full_name, external_party_name, is_pic
            FROM dbo.qa_calib_workers
            WHERE actual_id = @ActualId
            ORDER BY is_pic DESC, id
            """,
            new { ActualId = actualId });
        var items = await GetPlanItemsAsync(connection, header.HeaderId);
        return new ActualDetailDto(header.Id, header.PlanId, header.HeaderId, header.CalibNo, header.LinkedPlanNo, header.CalibMonth, header.CalibYear, header.CalibType, header.CompletionStatus, header.Remarks, header.CreatedBy, header.CreatedAt, header.UpdatedAt, items.Sum(x => x.ItemCount), items.Sum(x => x.Details.Count(x => !string.IsNullOrWhiteSpace(x.CalibResult))), approvals, workers, items);
    }

    public Task<ApiResponse<long>> CreateActualAsync(CreateActualRequest request, string createdBy)
        => CreateActualFromPlanInternalAsync(null, request, createdBy);

    public async Task<bool> UpdateActualHeaderAsync(long actualId, string? remarks, string updatedBy)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteAsync(
            """
            UPDATE h
            SET h.remarks = @Remarks,
                h.updated_at = sysutcdatetime(),
                h.updated_by = @UpdatedBy
            FROM dbo.qa_calib_main_headers h
            INNER JOIN dbo.qa_calib_actuals a ON a.header_id = h.id
            WHERE a.id = @ActualId AND a.calib_status = 'G'
            """,
            new { ActualId = actualId, Remarks = remarks, UpdatedBy = updatedBy }) > 0;
    }

    public async Task<ApiResponse<ActualWorkerDto>> AddWorkerAsync(long actualId, SaveActualWorkerRequest request, string createdBy)
    {
        using var connection = CreateConnection();
        var actualExists = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.qa_calib_actuals WHERE id = @ActualId", new { ActualId = actualId });
        if (actualExists == 0)
            return ApiResponse<ActualWorkerDto>.NotFound("Actual calibration not found.");

        EmployeeIdentityRow? employee = null;
        if (request.EmployeeId.HasValue)
        {
            employee = await connection.QuerySingleOrDefaultAsync<EmployeeIdentityRow>("SELECT employee_id, employee_code, full_name FROM Shared.dbo.employees WHERE employee_id = @EmployeeId", new { request.EmployeeId });
            if (employee is null)
                return ApiResponse<ActualWorkerDto>.Fail("Selected employee was not found.");
        }

        if (request.IsPic)
        {
            await connection.ExecuteAsync("UPDATE dbo.qa_calib_workers SET is_pic = 0 WHERE actual_id = @ActualId", new { ActualId = actualId });
        }

        var id = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO dbo.qa_calib_workers (actual_id, employee_id, employee_code, employee_full_name, external_party_name, is_pic, created_at, created_by)
            VALUES (@ActualId, @EmployeeId, @EmployeeCode, @EmployeeFullName, @ExternalPartyName, @IsPic, sysutcdatetime(), @CreatedBy);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                ActualId = actualId,
                EmployeeId = employee?.EmployeeId,
                EmployeeCode = employee?.EmployeeCode,
                EmployeeFullName = employee?.FullName,
                request.ExternalPartyName,
                request.IsPic,
                CreatedBy = createdBy
            });

            var worker = await connection.QuerySingleAsync<ActualWorkerDto>("SELECT id, employee_id, employee_code, employee_full_name, external_party_name, is_pic FROM dbo.qa_calib_workers WHERE id = @Id", new { Id = id });
            return ApiResponse<ActualWorkerDto>.Created(worker);
    }

    public async Task<ApiResponse> UpdateActualItemAsync(long itemId, UpdateActualItemRequest request, string updatedBy)
    {
        using var connection = CreateConnection();
        var affected = await connection.ExecuteAsync("UPDATE dbo.qa_calib_items SET std_used = @StdUsed, remarks = @Remarks, updated_at = sysutcdatetime(), updated_by = @UpdatedBy WHERE id = @ItemId", new { ItemId = itemId, request.StdUsed, request.Remarks, UpdatedBy = updatedBy });
        return affected > 0 ? ApiResponse.Ok("Updated successfully.") : ApiResponse.NotFound("Actual item not found.");
    }

    public async Task<ApiResponse> UpdateActualItemDetailAsync(long detailId, UpdateActualItemDetailRequest request, string updatedBy)
    {
        using var connection = CreateConnection();
        var detail = await connection.QuerySingleOrDefaultAsync<(long ItemId, long EquipmentId)>("SELECT item_id, equipment_id FROM dbo.qa_calib_item_details WHERE id = @DetailId", new { DetailId = detailId });
        if (detail == default)
            return ApiResponse.NotFound("Actual item detail not found.");

        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(
            """
            UPDATE dbo.qa_calib_item_details
            SET calib_result = @CalibResult,
                overdue_flag = @OverdueFlag,
                certificate_no = @CertificateNo,
                remarks = @Remarks,
                updated_at = sysutcdatetime(),
                updated_by = @UpdatedBy
            WHERE id = @DetailId
            """,
            new { DetailId = detailId, request.CalibResult, request.OverdueFlag, request.CertificateNo, request.Remarks, UpdatedBy = updatedBy },
            transaction);

        await connection.ExecuteAsync(
            """
            UPDATE dbo.qa_calib_items
            SET item_completed = (
                SELECT COUNT(*) FROM dbo.qa_calib_item_details WHERE item_id = @ItemId AND calib_result IS NOT NULL
            ),
            updated_at = sysutcdatetime(),
            updated_by = @UpdatedBy
            WHERE id = @ItemId
            """,
            new { detail.ItemId, UpdatedBy = updatedBy },
            transaction);

        await EnsureEquipmentSnapshotAsync(connection, transaction, detailId, detail.EquipmentId);
        transaction.Commit();
        return ApiResponse.Ok("Updated successfully.");
    }

    public async Task<ApiResponse> CompleteActualAsync(long actualId, string completedBy)
    {
        using var connection = CreateConnection();
        var status = await connection.QuerySingleOrDefaultAsync<(string CalibStatus, long HeaderId)>("SELECT calib_status, header_id FROM dbo.qa_calib_actuals WHERE id = @ActualId", new { ActualId = actualId });
        if (status == default)
            return ApiResponse.NotFound("Actual calibration not found.");

        var incompleteCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM dbo.qa_calib_item_details d
            INNER JOIN dbo.qa_calib_items i ON i.id = d.item_id
            WHERE i.header_id = @HeaderId
              AND d.calib_result IS NULL
            """,
            new { status.HeaderId });

        if (incompleteCount > 0)
            return ApiResponse.Fail("Actual calibration cannot be completed until all unit results are filled.");

        await connection.ExecuteAsync("UPDATE dbo.qa_calib_actuals SET calib_status = 'X', completed_dt = sysutcdatetime(), completed_by = @CompletedBy WHERE id = @ActualId", new { ActualId = actualId, CompletedBy = completedBy });
        return ApiResponse.Ok("Actual calibration marked as completed.");
    }

    public async Task<ApiResponse> SubmitActualAsync(long actualId)
    {
        using var connection = CreateConnection();
        var actual = await connection.QuerySingleOrDefaultAsync<(string Status, long HeaderId)>("SELECT calib_status AS Status, header_id AS HeaderId FROM dbo.qa_calib_actuals WHERE id = @ActualId", new { ActualId = actualId });
        if (actual == default)
            return ApiResponse.NotFound("Actual calibration not found.");

        if (actual.Status != "X")
            return ApiResponse.Fail("Actual calibration must be completed before approval submission.");

        await connection.ExecuteAsync("UPDATE dbo.qa_calib_approvals SET action = 'C', remarks = NULL, actioned_at = NULL, updated_at = NULL, updated_by = NULL WHERE header_id = @HeaderId", new { actual.HeaderId });
        return ApiResponse.Ok("Actual calibration submitted for approval.");
    }

    public async Task<ApiResponse> ActOnActualApprovalAsync(long actualId, long actorEmployeeId, string action, string? remarks, string updatedBy)
    {
        using var connection = CreateConnection();
        var actual = await connection.QuerySingleOrDefaultAsync<(long HeaderId, string Status)>("SELECT header_id AS HeaderId, calib_status AS Status FROM dbo.qa_calib_actuals WHERE id = @ActualId", new { ActualId = actualId });
        if (actual == default)
            return ApiResponse.NotFound("Actual calibration not found.");

        if (actual.Status != "X")
            return ApiResponse.Fail("Actual calibration must be completed before approval.");

        var approvals = (await connection.QueryAsync<ApprovalStepDto>(
            "SELECT step_no, CASE step_no WHEN '1' THEN 'Prepared' WHEN '2' THEN 'Checked' ELSE 'Approved' END AS step_name, employee_id, employee_code, employee_full_name, action, remarks, actioned_at FROM dbo.qa_calib_approvals WHERE header_id = @HeaderId ORDER BY step_no",
            new { actual.HeaderId })).ToList();

        var current = approvals.FirstOrDefault(x => x.EmployeeId == actorEmployeeId);
        if (current is null)
            return ApiResponse.Fail("You are not assigned to this actual approval chain.");

        var previousPending = approvals.Where(x => string.CompareOrdinal(x.StepNo, current.StepNo) < 0).Any(x => x.Action != "S" || !x.ActionedAt.HasValue);
        if (previousPending)
            return ApiResponse.Fail("Previous approval steps must be completed first.");

        if (action == "C")
        {
            var canCancel = CanCancelApproval(current.StepNo, current.Action, current.ActionedAt, approvals);
            if (!canCancel.Success)
                return ApiResponse.Fail(canCancel.Message);
        }

        await connection.ExecuteAsync(
            """
            UPDATE dbo.qa_calib_approvals
            SET action = @Action,
                remarks = @Remarks,
                actioned_at = sysutcdatetime(),
                updated_at = sysutcdatetime(),
                updated_by = @UpdatedBy
            WHERE header_id = @HeaderId AND employee_id = @EmployeeId
            """,
            new { HeaderId = actual.HeaderId, EmployeeId = actorEmployeeId, Action = action, Remarks = remarks, UpdatedBy = updatedBy });

        if (action == "C")
            return ApiResponse.Ok("Actual approval was cancelled.");

        var refreshed = (await connection.QueryAsync<ApprovalStepDto>(
            "SELECT step_no, CASE step_no WHEN '1' THEN 'Prepared' WHEN '2' THEN 'Checked' ELSE 'Approved' END AS step_name, employee_id, employee_code, employee_full_name, action, remarks, actioned_at FROM dbo.qa_calib_approvals WHERE header_id = @HeaderId ORDER BY step_no",
            new { actual.HeaderId })).ToList();

        return refreshed.All(x => x.Action == "S" && x.ActionedAt.HasValue)
            ? ApiResponse.Ok("Actual calibration fully approved.")
            : ApiResponse.Ok("Approval action saved.");
    }

    private async Task<ApiResponse<long>> CreateActualFromPlanInternalAsync(long? planId, CreateActualRequest request, string createdBy)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var actualPlanId = planId ?? request.PlanId;
        var planHeaderId = await connection.QuerySingleOrDefaultAsync<long?>("SELECT header_id FROM dbo.qa_calib_plans WHERE id = @PlanId", new { PlanId = actualPlanId }, transaction);
        if (!planHeaderId.HasValue)
            return ApiResponse<long>.NotFound("Source plan was not found.");

        var planHeader = await connection.QuerySingleOrDefaultAsync<(string CalibNo, int CalibMonth, int CalibYear, string CalibType)>(
            "SELECT calib_no, calib_month, calib_year, calib_type FROM dbo.qa_calib_main_headers WHERE id = @HeaderId",
            new { HeaderId = planHeaderId.Value },
            transaction);
        if (planHeader == default)
            return ApiResponse<long>.NotFound("Source plan was not found.");

        var existingActualId = await connection.QuerySingleOrDefaultAsync<long?>(
            "SELECT id FROM dbo.qa_calib_actuals WHERE plan_id = @PlanId",
            new { PlanId = actualPlanId },
            transaction);

        if (existingActualId.HasValue)
        {
            transaction.Commit();
            return ApiResponse<long>.Ok(existingActualId.Value, "Actual calibration already exists for this plan.");
        }

        var actualHeaderId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO dbo.qa_calib_main_headers (calib_no, calib_phase, calib_type, calib_month, calib_year, remarks, created_at, updated_at, created_by, updated_by)
            VALUES (@CalibNo, 'A', @CalibType, @CalibMonth, @CalibYear, @Remarks, sysutcdatetime(), NULL, @CreatedBy, NULL);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                CalibNo = await GenerateCalibNoAsync(connection, transaction, "A", planHeader.CalibYear, planHeader.CalibMonth),
                planHeader.CalibType,
                planHeader.CalibMonth,
                planHeader.CalibYear,
                request.Remarks,
                CreatedBy = createdBy
            },
            transaction);

        var actualId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO dbo.qa_calib_actuals (plan_id, header_id, calib_status)
            VALUES (@PlanId, @HeaderId, 'G');
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new { PlanId = actualPlanId, HeaderId = actualHeaderId },
            transaction);

        await InsertApprovalRowsAsync(connection, transaction, actualHeaderId, request.PreparerEmployeeId, request.CheckerEmployeeId, request.ApproverEmployeeId, createdBy);
        await CopyPlanItemsToActualAsync(connection, transaction, planHeaderId.Value, actualHeaderId, createdBy);

        transaction.Commit();
        return ApiResponse<long>.Created(actualId);
    }

    private async Task<PlanDetailDto> BuildPlanDetailAsync(IDbConnection connection, long planId, long headerId, string calibNo, int calibMonth, int calibYear, string calibType, string status, string? remarks, string createdBy, DateTime createdAt, DateTime? updatedAt)
    {
        var approvals = await GetApprovalsAsync(connection, headerId);
        var items = await GetPlanItemsAsync(connection, headerId);
        var hasActual = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.qa_calib_actuals WHERE plan_id = @PlanId", new { PlanId = planId }) > 0;
        return new PlanDetailDto(planId, headerId, calibNo, calibMonth, calibYear, calibType, status, remarks, createdBy, createdAt, updatedAt, approvals, items, hasActual);
    }

    public async Task<ApiResponse<DeleteDocumentResponse>> DeletePlanAsync(long planId, string requestedBy)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var plan = await connection.QuerySingleOrDefaultAsync<(long HeaderId, string CalibNo)>(
            """
            SELECT p.header_id, h.calib_no
            FROM dbo.qa_calib_plans p
            INNER JOIN dbo.qa_calib_main_headers h ON h.id = p.header_id
            WHERE p.id = @PlanId
            """,
            new { PlanId = planId },
            transaction);
        if (plan == default)
            return ApiResponse<DeleteDocumentResponse>.NotFound("Plan not found.");

        var actualExists = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.qa_calib_actuals WHERE plan_id = @PlanId", new { PlanId = planId }, transaction) > 0;
        if (actualExists)
            return ApiResponse<DeleteDocumentResponse>.Fail("Plan cannot be deleted because an actual calibration already exists for it.");

        await connection.ExecuteAsync("DELETE FROM dbo.qa_calib_main_headers WHERE id = @HeaderId", new { plan.HeaderId }, transaction);
        transaction.Commit();
        return ApiResponse<DeleteDocumentResponse>.Ok(new DeleteDocumentResponse(planId, plan.CalibNo, $"Plan deleted by {requestedBy}."), "Plan deleted successfully.");
    }

    public async Task<ApiResponse<DeleteDocumentResponse>> DeleteActualAsync(long actualId, string requestedBy)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var actual = await connection.QuerySingleOrDefaultAsync<(long HeaderId, long PlanId, string CalibNo)>(
            """
            SELECT a.header_id, a.plan_id, h.calib_no
            FROM dbo.qa_calib_actuals a
            INNER JOIN dbo.qa_calib_main_headers h ON h.id = a.header_id
            WHERE a.id = @ActualId
            """,
            new { ActualId = actualId },
            transaction);
        if (actual == default)
            return ApiResponse<DeleteDocumentResponse>.NotFound("Actual calibration not found.");

        var approvals = (await connection.QueryAsync<ApprovalStepDto>(
            "SELECT step_no, CASE step_no WHEN '1' THEN 'Prepared' WHEN '2' THEN 'Checked' ELSE 'Approved' END AS step_name, employee_id, employee_code, employee_full_name, action, remarks, actioned_at FROM dbo.qa_calib_approvals WHERE header_id = @HeaderId ORDER BY step_no",
            new { actual.HeaderId },
            transaction)).ToList();

        var blockingApproval = approvals.FirstOrDefault(x => x.Action == "S" && x.ActionedAt.HasValue && !CanCancelApproval(x.StepNo, x.Action, x.ActionedAt, approvals).Success);
        if (blockingApproval is not null)
            return ApiResponse<DeleteDocumentResponse>.Fail("Actual calibration cannot be deleted while it has approvals that are no longer cancelable.");

        await connection.ExecuteAsync("DELETE FROM dbo.qa_calib_main_headers WHERE id = @HeaderId", new { actual.HeaderId }, transaction);
        transaction.Commit();
        return ApiResponse<DeleteDocumentResponse>.Ok(new DeleteDocumentResponse(actualId, actual.CalibNo, $"Actual deleted by {requestedBy}."), "Actual calibration deleted successfully.");
    }

    private static async Task<List<ApprovalStepDto>> GetApprovalsAsync(IDbConnection connection, long headerId)
        => (await connection.QueryAsync<ApprovalStepDto>(
            """
            SELECT
                step_no,
                CASE step_no WHEN '1' THEN 'Prepared' WHEN '2' THEN 'Checked' ELSE 'Approved' END AS step_name,
                employee_id,
                employee_code,
                employee_full_name,
                action,
                remarks,
                actioned_at
            FROM dbo.qa_calib_approvals
            WHERE header_id = @HeaderId
            ORDER BY step_no
            """,
            new { HeaderId = headerId })).ToList();

    private static async Task<List<PlanItemDto>> GetPlanItemsAsync(IDbConnection connection, long headerId)
    {
        var items = (await connection.QueryAsync<PlanItemRow>(
            "SELECT id, equipment_name, item_count, item_completed, std_used, remarks FROM dbo.qa_calib_items WHERE header_id = @HeaderId ORDER BY equipment_name, id",
            new { HeaderId = headerId })).ToList();

        var details = (await connection.QueryAsync<PlanItemDetailRow>(
            """
            SELECT
                d.id,
                d.equipment_id,
                e.control_no,
                s.section_name,
                e.pic_full_name,
                e.next_calib_date,
                d.calib_result,
                d.overdue_flag,
                d.certificate_no,
                d.remarks
            FROM dbo.qa_calib_item_details d
            INNER JOIN dbo.qa_calib_equipments e ON e.id = d.equipment_id
            INNER JOIN dbo.sections s ON s.section_id = e.section_id
            INNER JOIN dbo.qa_calib_items i ON i.id = d.item_id
            WHERE i.header_id = @HeaderId
            ORDER BY i.id, e.control_no
            """,
            new { HeaderId = headerId }))
            .Select(x => new PlanItemDetailDto(x.Id, x.EquipmentId, x.ControlNo, x.SectionName, x.PicFullName, x.NextCalibDate, x.CalibResult, x.OverdueFlag, x.CertificateNo, x.Remarks))
            .ToList();

        var itemDetails = await connection.QueryAsync<(long ItemId, long DetailId)>("SELECT item_id, id AS detail_id FROM dbo.qa_calib_item_details d INNER JOIN dbo.qa_calib_items i ON i.id = d.item_id WHERE i.header_id = @HeaderId", new { HeaderId = headerId });
        var detailMap = itemDetails.GroupBy(x => x.ItemId).ToDictionary(x => x.Key, x => x.Select(v => v.DetailId).ToHashSet());

        return items.Select(item =>
        {
            detailMap.TryGetValue(item.Id, out var ids);
            var itemRows = details.Where(d => ids?.Contains(d.Id) == true).ToList();
            return new PlanItemDto(item.Id, item.EquipmentName, item.ItemCount, item.ItemCompleted, item.StdUsed, item.Remarks, itemRows);
        }).ToList();
    }

    private static async Task InsertApprovalRowsAsync(IDbConnection connection, IDbTransaction transaction, long headerId, long preparerEmployeeId, long checkerEmployeeId, long approverEmployeeId, string createdBy)
    {
        foreach (var step in new[] { ("1", preparerEmployeeId), ("2", checkerEmployeeId), ("3", approverEmployeeId) })
        {
            var employee = await connection.QuerySingleAsync<EmployeeIdentityRow>("SELECT employee_id, employee_code, full_name FROM Shared.dbo.employees WHERE employee_id = @EmployeeId", new { EmployeeId = step.Item2 }, transaction);
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.qa_calib_approvals (header_id, step_no, employee_id, employee_code, employee_full_name, action, remarks, actioned_at, created_at, created_by, updated_at, updated_by)
                VALUES (@HeaderId, @StepNo, @EmployeeId, @EmployeeCode, @EmployeeFullName, 'C', NULL, NULL, sysutcdatetime(), @CreatedBy, NULL, NULL)
                """,
                new { HeaderId = headerId, StepNo = step.Item1, employee.EmployeeId, employee.EmployeeCode, EmployeeFullName = employee.FullName, CreatedBy = createdBy },
                transaction);
        }
    }

    private static async Task InsertPlanItemsAsync(IDbConnection connection, IDbTransaction transaction, long headerId, IEnumerable<long> equipmentIds, string createdBy)
    {
        var equipmentRows = (await connection.QueryAsync<EquipmentPlanRow>(
            """
            SELECT id, equipment_name, control_no, next_calib_date
            FROM dbo.qa_calib_equipments
            WHERE id IN @EquipmentIds
            ORDER BY equipment_name, control_no
            """,
            new { EquipmentIds = equipmentIds.Distinct().ToArray() },
            transaction)).ToList();

        foreach (var group in equipmentRows.GroupBy(x => x.EquipmentName))
        {
            var itemId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO dbo.qa_calib_items (header_id, equipment_name, item_count, item_completed, std_used, remarks, created_at, updated_at, created_by, updated_by)
                VALUES (@HeaderId, @EquipmentName, @ItemCount, 0, NULL, NULL, sysutcdatetime(), NULL, @CreatedBy, NULL);
                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """,
                new { HeaderId = headerId, EquipmentName = group.Key, ItemCount = group.Count(), CreatedBy = createdBy },
                transaction);

            foreach (var equipment in group)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO dbo.qa_calib_item_details (item_id, equipment_id, calib_result, overdue_flag, certificate_no, remarks, created_at, updated_at, created_by, updated_by)
                    VALUES (@ItemId, @EquipmentId, NULL, @OverdueFlag, NULL, NULL, sysutcdatetime(), NULL, @CreatedBy, NULL)
                    """,
                    new
                    {
                        ItemId = itemId,
                        EquipmentId = equipment.Id,
                        OverdueFlag = equipment.NextCalibDate < DateOnly.FromDateTime(DateTime.Today),
                        CreatedBy = createdBy
                    },
                    transaction);
            }
        }
    }

    private static async Task CopyPlanItemsToActualAsync(IDbConnection connection, IDbTransaction transaction, long sourceHeaderId, long targetHeaderId, string createdBy)
    {
        var items = (await connection.QueryAsync<PlanItemRow>("SELECT id, equipment_name, item_count, item_completed, std_used, remarks FROM dbo.qa_calib_items WHERE header_id = @HeaderId", new { HeaderId = sourceHeaderId }, transaction)).ToList();
        foreach (var item in items)
        {
            var newItemId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO dbo.qa_calib_items (header_id, equipment_name, item_count, item_completed, std_used, remarks, created_at, updated_at, created_by, updated_by)
                VALUES (@HeaderId, @EquipmentName, @ItemCount, 0, @StdUsed, @Remarks, sysutcdatetime(), NULL, @CreatedBy, NULL);
                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """,
                new { HeaderId = targetHeaderId, item.EquipmentName, item.ItemCount, item.StdUsed, item.Remarks, CreatedBy = createdBy },
                transaction);

            var details = await connection.QueryAsync<(long EquipmentId, bool OverdueFlag)>("SELECT equipment_id, overdue_flag FROM dbo.qa_calib_item_details WHERE item_id = @ItemId", new { ItemId = item.Id }, transaction);
            foreach (var detail in details)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO dbo.qa_calib_item_details (item_id, equipment_id, calib_result, overdue_flag, certificate_no, remarks, created_at, updated_at, created_by, updated_by)
                    VALUES (@ItemId, @EquipmentId, NULL, @OverdueFlag, NULL, NULL, sysutcdatetime(), NULL, @CreatedBy, NULL)
                    """,
                    new { ItemId = newItemId, detail.EquipmentId, detail.OverdueFlag, CreatedBy = createdBy },
                    transaction);
            }
        }
    }

    private static async Task EnsureEquipmentSnapshotAsync(IDbConnection connection, IDbTransaction transaction, long detailId, long equipmentId)
    {
        var exists = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.qa_calib_equipment_details WHERE detail_id = @DetailId", new { DetailId = detailId }, transaction);
        if (exists > 0)
            return;

        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.qa_calib_equipment_details (detail_id, equipment_id, equipment_name, control_no, serial_no, brand, model, location, section_code, section_name, calib_interval_months, last_calib_date, last_calib_month, next_calib_date, next_calib_month, pic_code, pic_full_name)
            SELECT
                @DetailId,
                e.id,
                e.equipment_name,
                e.control_no,
                e.serial_no,
                e.brand,
                e.model,
                e.location,
                s.section_code,
                s.section_name,
                e.calib_interval_months,
                e.last_calib_date,
                e.last_calib_month,
                e.next_calib_date,
                e.next_calib_month,
                e.pic_code,
                e.pic_full_name
            FROM dbo.qa_calib_equipments e
            INNER JOIN dbo.sections s ON s.section_id = e.section_id
            WHERE e.id = @EquipmentId
            """,
            new { DetailId = detailId, EquipmentId = equipmentId },
            transaction);
    }

    private static async Task<string> GenerateCalibNoAsync(IDbConnection connection, IDbTransaction transaction, string phase, int year, int month)
    {
        var prefix = phase == "P" ? "PLAN" : "ACT";
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.qa_calib_main_headers WHERE calib_phase = @Phase AND calib_year = @Year AND calib_month = @Month",
            new { Phase = phase, Year = year, Month = month },
            transaction);
        return $"{prefix}-{year}{month:00}-{count + 1:000}";
    }

    private static (bool Success, string Message) CanCancelApproval(string stepNo, string action, DateTime? actionedAt, IReadOnlyCollection<ApprovalStepDto> approvals)
    {
        if (action != "S" || !actionedAt.HasValue)
            return (false, "Only an already-approved step can be cancelled.");

        return stepNo switch
        {
            "1" when approvals.Any(x => x.StepNo is "2" or "3" && x.Action == "S" && x.ActionedAt.HasValue)
                => (false, "Preparer approval can no longer be cancelled after a later step has approved."),
            "2" when approvals.Any(x => x.StepNo == "3" && x.Action == "S" && x.ActionedAt.HasValue)
                => (false, "Checker approval can no longer be cancelled after the approver has approved."),
            "3" when DateTime.UtcNow > actionedAt.Value.AddDays(1)
                => (false, "Approver cancellation window has expired."),
            _ => (true, string.Empty)
        };
    }

    private sealed record PlanDetailHeaderRow(long Id, long HeaderId, string CalibNo, int CalibMonth, int CalibYear, string CalibType, string Status, string? Remarks, string CreatedBy, DateTime CreatedAt, DateTime? UpdatedAt);
    private sealed record ActualDetailHeaderRow(long Id, long PlanId, long HeaderId, string CalibNo, string? LinkedPlanNo, int CalibMonth, int CalibYear, string CalibType, string CompletionStatus, string? Remarks, string CreatedBy, DateTime CreatedAt, DateTime? UpdatedAt);
    private sealed record EmployeeIdentityRow(long EmployeeId, string EmployeeCode, string FullName);
    private sealed record EquipmentPlanRow(long Id, string EquipmentName, string ControlNo, DateOnly NextCalibDate);
    private sealed record PlanItemRow(long Id, string EquipmentName, int ItemCount, int ItemCompleted, string? StdUsed, string? Remarks);
    private sealed record PlanItemDetailRow(int Id, int EquipmentId, string ControlNo, string SectionName, string PicFullName, DateTime NextCalibDate, string? CalibResult, bool OverdueFlag, string? CertificateNo, string? Remarks);
}
