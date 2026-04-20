using System.Data;
using Dapper;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed partial class CalibrationRepository : BaseRepository<CalibrationEquipment>, ICalibrationRepository
{
    public CalibrationRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(int? year, long? sectionId, string? status)
    {
        using var connection = CreateConnection();
        var parameters = new { Year = year ?? DateTime.UtcNow.Year, SectionId = sectionId, Status = status };

        var equipmentsBySection = await connection.QueryAsync<DashboardCountItemDto>(
            """
            SELECT s.section_name AS label, COUNT(*) AS total
            FROM dbo.qa_calib_equipments e
            INNER JOIN dbo.sections s ON s.section_id = e.section_id
            WHERE (@SectionId IS NULL OR e.section_id = @SectionId)
            GROUP BY s.section_name
            ORDER BY s.section_name
            """,
            parameters);

        var equipmentsByType = await connection.QueryAsync<DashboardCountItemDto>(
            """
            SELECT CASE e.calib_type WHEN 'I' THEN 'Internal' ELSE 'External' END AS label, COUNT(*) AS total
            FROM dbo.qa_calib_equipments e
            WHERE (@SectionId IS NULL OR e.section_id = @SectionId)
            GROUP BY e.calib_type
            ORDER BY label
            """,
            parameters);

        var equipmentsByName = await connection.QueryAsync<DashboardCountItemDto>(
            """
            SELECT e.equipment_name AS label, COUNT(*) AS total
            FROM dbo.qa_calib_equipments e
            WHERE (@SectionId IS NULL OR e.section_id = @SectionId)
            GROUP BY e.equipment_name
            ORDER BY total DESC, e.equipment_name
            """,
            parameters);

        var overviewByPeriod = await connection.QueryAsync<DashboardGroupedItemDto>(
            """
            SELECT
                e.equipment_name AS label,
                CASE
                    WHEN e.next_calib_date < DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) THEN 'Overdue'
                    WHEN e.next_calib_date < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) THEN 'Due This Month'
                    WHEN e.next_calib_date < DATEADD(MONTH, 2, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) THEN 'Next Month'
                    ELSE 'Later'
                END AS [group],
                COUNT(*) AS total
            FROM dbo.qa_calib_equipments e
            WHERE (@SectionId IS NULL OR e.section_id = @SectionId)
            GROUP BY e.equipment_name,
                CASE
                    WHEN e.next_calib_date < DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) THEN 'Overdue'
                    WHEN e.next_calib_date < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) THEN 'Due This Month'
                    WHEN e.next_calib_date < DATEADD(MONTH, 2, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) THEN 'Next Month'
                    ELSE 'Later'
                END
            ORDER BY e.equipment_name, [group]
            """,
            parameters);

        var performance = await connection.QueryAsync<DashboardMonthlyPlanActualDto>(
            """
            WITH months AS (
                SELECT 1 AS [month] UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6
                UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12
            )
            SELECT
                m.[month],
                COALESCE(p.planned_total, 0) AS planned_total,
                COALESCE(a.actual_total, 0) AS actual_total
            FROM months m
            LEFT JOIN (
                SELECT h.calib_month, COUNT(*) AS planned_total
                FROM dbo.qa_calib_main_headers h
                WHERE h.calib_phase = 'P' AND h.calib_year = @Year
                GROUP BY h.calib_month
            ) p ON p.calib_month = m.[month]
            LEFT JOIN (
                SELECT h.calib_month, COUNT(*) AS actual_total
                FROM dbo.qa_calib_main_headers h
                WHERE h.calib_phase = 'A' AND h.calib_year = @Year
                GROUP BY h.calib_month
            ) a ON a.calib_month = m.[month]
            ORDER BY m.[month]
            """,
            parameters);

        var statusOverview = await connection.QueryAsync<DashboardCountItemDto>(
            """
            SELECT
                CASE equipment_status WHEN 'A' THEN 'Active' WHEN 'O' THEN 'Out of Service' ELSE 'Scrapped' END AS label,
                COUNT(*) AS total
            FROM dbo.qa_calib_equipments
            WHERE (@SectionId IS NULL OR section_id = @SectionId)
            GROUP BY equipment_status
            ORDER BY label
            """,
            parameters);

        var outOfServiceBySection = await connection.QueryAsync<DashboardGroupedItemDto>(
            """
            SELECT s.section_name AS label, 'Out of Service' AS [group], COUNT(*) AS total
            FROM dbo.qa_calib_equipments e
            INNER JOIN dbo.sections s ON s.section_id = e.section_id
            WHERE e.equipment_status = 'O'
              AND (@SectionId IS NULL OR e.section_id = @SectionId)
            GROUP BY s.section_name
            ORDER BY s.section_name
            """,
            parameters);

        var statusBySection = await connection.QueryAsync<DashboardGroupedItemDto>(
            """
            SELECT
                s.section_name AS label,
                CASE e.equipment_status WHEN 'A' THEN 'Active' ELSE 'Scrapped' END AS [group],
                COUNT(*) AS total
            FROM dbo.qa_calib_equipments e
            INNER JOIN dbo.sections s ON s.section_id = e.section_id
            WHERE e.equipment_status IN ('A', 'S')
              AND (@SectionId IS NULL OR e.section_id = @SectionId)
              AND (@Status IS NULL OR e.equipment_status = @Status)
            GROUP BY s.section_name, CASE e.equipment_status WHEN 'A' THEN 'Active' ELSE 'Scrapped' END
            ORDER BY s.section_name, [group]
            """,
            parameters);

        var reminderItems = (await GetReminderItemsAsync(connection, new ReminderFilterParams { Page = 1, PageSize = 200, SectionId = sectionId })).ToList();
        var reminderSummary = reminderItems
            .GroupBy(x => x.DueCategory)
            .Select(x => new DashboardReminderSummaryDto(x.Key, x.Count()))
            .OrderBy(x => x.DueCategory)
            .ToList();

        return new DashboardOverviewDto(
            equipmentsBySection,
            equipmentsByType,
            equipmentsByName,
            overviewByPeriod,
            performance,
            statusOverview,
            outOfServiceBySection,
            statusBySection,
            reminderSummary,
            reminderItems.Take(25).ToList(),
            reminderItems);
    }

    public async Task<(IEnumerable<ApproverDto> Items, int TotalCount)> GetApproversAsync(ApproverFilterParams filters)
    {
        var where = new List<string> { "1 = 1" };
        var parameters = new DynamicParameters(new { filters.Offset, filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(e.employee_code LIKE @Search OR e.full_name LIKE @Search OR s.section_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filters.StepNo))
        {
            where.Add("a.step_no = @StepNo");
            parameters.Add("StepNo", filters.StepNo.Trim());
        }
        if (filters.SectionId.HasValue)
        {
            where.Add("e.section_id = @SectionId");
            parameters.Add("SectionId", filters.SectionId.Value);
        }
        if (filters.IsActive.HasValue)
        {
            where.Add("a.is_active = @IsActive");
            parameters.Add("IsActive", filters.IsActive.Value);
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM dbo.qa_calib_approvers a
            INNER JOIN dbo.employees e ON e.employee_id = a.employee_id
            INNER JOIN dbo.sections s ON s.section_id = e.section_id
            WHERE {whereClause}
            """, parameters);
        var items = await connection.QueryAsync<ApproverDto>($"""
            SELECT
                a.id,
                a.employee_id,
                e.employee_code,
                e.full_name AS employee_name,
                e.section_id,
                s.section_name,
                a.step_no,
                a.is_active,
                a.created_at,
                a.updated_at,
                a.created_by,
                a.updated_by
            FROM dbo.qa_calib_approvers a
            INNER JOIN dbo.employees e ON e.employee_id = a.employee_id
            INNER JOIN dbo.sections s ON s.section_id = e.section_id
            WHERE {whereClause}
            ORDER BY a.step_no, e.full_name
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public async Task<ApproverDto?> GetApproverByIdAsync(long approverId)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ApproverDto>(
            """
            SELECT
                a.id,
                a.employee_id,
                e.employee_code,
                e.full_name AS employee_name,
                e.section_id,
                s.section_name,
                a.step_no,
                a.is_active,
                a.created_at,
                a.updated_at,
                a.created_by,
                a.updated_by
            FROM dbo.qa_calib_approvers a
            INNER JOIN dbo.employees e ON e.employee_id = a.employee_id
            INNER JOIN dbo.sections s ON s.section_id = e.section_id
            WHERE a.id = @ApproverId
            """,
            new { ApproverId = approverId });
    }

    public async Task<bool> ApproverAssignmentExistsAsync(long employeeId, string stepNo, long? excludeId = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.qa_calib_approvers WHERE employee_id = @EmployeeId AND step_no = @StepNo AND (@ExcludeId IS NULL OR id <> @ExcludeId)",
            new { EmployeeId = employeeId, StepNo = stepNo, ExcludeId = excludeId }) > 0;
    }

    public Task<long> CreateApproverAsync(CalibrationApprover approver)
        => ExecuteScalarAsync<long>("INSERT INTO dbo.qa_calib_approvers (employee_id, step_no, is_active, created_at, updated_at, created_by, updated_by) VALUES (@EmployeeId, @StepNo, @IsActive, @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy); SELECT CAST(SCOPE_IDENTITY() AS bigint);", approver)!;

    public async Task<bool> UpdateApproverAsync(CalibrationApprover approver)
        => await ExecuteAsync("UPDATE dbo.qa_calib_approvers SET employee_id = @EmployeeId, step_no = @StepNo, is_active = @IsActive, updated_at = @UpdatedAt, updated_by = @UpdatedBy WHERE id = @Id", approver) > 0;

    public async Task<Employee?> GetEmployeeByIdAsync(long employeeId)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Employee>("SELECT employee_id, employee_code, full_name, email, section_id, position_id, manager_id, employment_status, is_active, created_at, updated_at FROM dbo.employees WHERE employee_id = @EmployeeId", new { EmployeeId = employeeId });
    }

    public async Task<EquipmentSummaryCardsDto> GetEquipmentSummaryCardsAsync()
    {
        using var connection = CreateConnection();
        var row = await connection.QuerySingleAsync<EquipmentSummaryCardsDto>(
            """
            SELECT
                COUNT(*) AS total_equipments,
                SUM(CASE WHEN equipment_status = 'A' THEN 1 ELSE 0 END) AS active,
                SUM(CASE WHEN equipment_status = 'O' THEN 1 ELSE 0 END) AS out_of_service,
                SUM(CASE WHEN equipment_status = 'S' THEN 1 ELSE 0 END) AS scrapped,
                SUM(CASE WHEN calib_type = 'I' THEN 1 ELSE 0 END) AS internal_calibration,
                SUM(CASE WHEN calib_type = 'E' THEN 1 ELSE 0 END) AS external_calibration,
                SUM(CASE WHEN next_calib_date >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                           AND next_calib_date < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) THEN 1 ELSE 0 END) AS due_this_month,
                SUM(CASE WHEN next_calib_date < DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) THEN 1 ELSE 0 END) AS overdue
            FROM dbo.qa_calib_equipments
            """);
        return row;
    }

    public async Task<IEnumerable<EquipmentGroupSummaryDto>> GetEquipmentGroupSummaryAsync(string? search, long? sectionId, string? dueCategory)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<EquipmentGroupSummaryDto>(
            """
            SELECT
                e.equipment_name,
                COUNT(*) AS total_quantity,
                SUM(CASE WHEN e.equipment_status = 'A' THEN 1 ELSE 0 END) AS active_count,
                SUM(CASE WHEN e.equipment_status = 'O' THEN 1 ELSE 0 END) AS out_of_service_count,
                SUM(CASE WHEN e.equipment_status = 'S' THEN 1 ELSE 0 END) AS scrapped_count,
                SUM(CASE WHEN e.next_calib_date >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                           AND e.next_calib_date < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) THEN 1 ELSE 0 END) AS due_this_month_count,
                SUM(CASE WHEN e.next_calib_date < DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) THEN 1 ELSE 0 END) AS overdue_count
            FROM dbo.qa_calib_equipments e
            WHERE (@Search IS NULL OR e.equipment_name LIKE @LikeSearch)
              AND (@SectionId IS NULL OR e.section_id = @SectionId)
              AND (
                    @DueCategory IS NULL
                 OR (@DueCategory = 'Due This Month' AND e.next_calib_date >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                                                AND e.next_calib_date < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)))
                 OR (@DueCategory = 'Overdue' AND e.next_calib_date < DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
                 OR (@DueCategory = 'Next Month' AND e.next_calib_date >= DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
                                              AND e.next_calib_date < DATEADD(MONTH, 2, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)))
                  )
            GROUP BY e.equipment_name
            ORDER BY e.equipment_name
            """,
            new { Search = search, LikeSearch = $"%{search?.Trim()}%", SectionId = sectionId, DueCategory = dueCategory });
    }

    public async Task<(IEnumerable<EquipmentListItemDto> Items, int TotalCount)> GetEquipmentsAsync(EquipmentFilterParams filters)
    {
        var where = new List<string> { "1 = 1" };
        var parameters = new DynamicParameters(new { filters.Offset, filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(e.control_no LIKE @Search OR e.equipment_name LIKE @Search OR e.serial_no LIKE @Search OR e.pic_full_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (filters.SectionId.HasValue)
        {
            where.Add("e.section_id = @SectionId");
            parameters.Add("SectionId", filters.SectionId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filters.Location))
        {
            where.Add("e.location LIKE @Location");
            parameters.Add("Location", $"%{filters.Location.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filters.EquipmentStatus))
        {
            where.Add("e.equipment_status = @EquipmentStatus");
            parameters.Add("EquipmentStatus", filters.EquipmentStatus.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filters.CalibType))
        {
            where.Add("e.calib_type = @CalibType");
            parameters.Add("CalibType", filters.CalibType.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filters.DueCategory))
        {
            where.Add(BuildDueCategorySql(filters.DueCategory.Trim()));
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM dbo.qa_calib_equipments e
            WHERE {whereClause}
            """, parameters);
        var items = await connection.QueryAsync<EquipmentListItemDto>($"""
            SELECT
                e.id,
                e.equipment_name,
                e.control_no,
                e.serial_no,
                e.brand,
                e.model,
                e.location,
                e.section_id,
                s.section_code,
                s.section_name,
                e.pic_id,
                e.pic_code,
                e.pic_full_name,
                e.calib_interval_months,
                e.last_calib_date,
                e.next_calib_date,
                e.calib_type,
                e.equipment_status,
                e.remarks
            FROM dbo.qa_calib_equipments e
            INNER JOIN dbo.sections s ON s.section_id = e.section_id
            WHERE {whereClause}
            ORDER BY e.id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public async Task<EquipmentDetailDto?> GetEquipmentByIdAsync(long equipmentId)
    {
        using var connection = CreateConnection();
        var equipment = await connection.QuerySingleOrDefaultAsync<EquipmentListItemDto>(
            """
            SELECT
                e.id,
                e.equipment_name,
                e.control_no,
                e.serial_no,
                e.brand,
                e.model,
                e.location,
                e.section_id,
                s.section_code,
                s.section_name,
                e.pic_id,
                e.pic_code,
                e.pic_full_name,
                e.calib_interval_months,
                e.last_calib_date,
                e.next_calib_date,
                e.calib_type,
                e.equipment_status,
                e.remarks
            FROM dbo.qa_calib_equipments e
            INNER JOIN dbo.sections s ON s.section_id = e.section_id
            WHERE e.id = @EquipmentId
            """,
            new { EquipmentId = equipmentId });

        if (equipment is null)
            return null;

        var history = await connection.QueryAsync<CalibrationHistoryEntryDto>(
            """
            SELECT
                h.id AS header_id,
                h.calib_no,
                h.calib_phase,
                h.calib_type,
                h.calib_month,
                h.calib_year,
                CASE
                    WHEN h.calib_phase = 'P' THEN p.calib_status
                    ELSE a.calib_status
                END AS document_status,
                d.calib_result,
                d.overdue_flag,
                d.certificate_no,
                d.remarks,
                h.created_at
            FROM dbo.qa_calib_item_details d
            INNER JOIN dbo.qa_calib_items i ON i.id = d.item_id
            INNER JOIN dbo.qa_calib_main_headers h ON h.id = i.header_id
            LEFT JOIN dbo.qa_calib_plans p ON p.header_id = h.id
            LEFT JOIN dbo.qa_calib_actuals a ON a.header_id = h.id
            WHERE d.equipment_id = @EquipmentId
            ORDER BY h.created_at DESC, h.id DESC
            """,
            new { EquipmentId = equipmentId });

        return new EquipmentDetailDto(equipment, history);
    }

    public async Task<bool> EquipmentControlNoExistsAsync(string controlNo, long? excludeId = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.qa_calib_equipments WHERE control_no = @ControlNo AND (@ExcludeId IS NULL OR id <> @ExcludeId)", new { ControlNo = controlNo, ExcludeId = excludeId }) > 0;
    }

    public Task<long> CreateEquipmentAsync(CalibrationEquipment equipment)
        => ExecuteScalarAsync<long>("INSERT INTO dbo.qa_calib_equipments (equipment_name, control_no, serial_no, brand, model, location, section_id, pic_id, pic_code, pic_full_name, calib_interval_months, last_calib_date, calib_type, equipment_status, remarks, created_at, updated_at, created_by, updated_by) VALUES (@EquipmentName, @ControlNo, @SerialNo, @Brand, @Model, @Location, @SectionId, @PicId, @PicCode, @PicFullName, @CalibIntervalMonths, @LastCalibDate, @CalibType, @EquipmentStatus, @Remarks, @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy); SELECT CAST(SCOPE_IDENTITY() AS bigint);", equipment)!;

    public async Task<bool> UpdateEquipmentAsync(CalibrationEquipment equipment)
        => await ExecuteAsync("UPDATE dbo.qa_calib_equipments SET equipment_name = @EquipmentName, control_no = @ControlNo, serial_no = @SerialNo, brand = @Brand, model = @Model, location = @Location, section_id = @SectionId, pic_id = @PicId, pic_code = @PicCode, pic_full_name = @PicFullName, calib_interval_months = @CalibIntervalMonths, last_calib_date = @LastCalibDate, calib_type = @CalibType, equipment_status = @EquipmentStatus, remarks = @Remarks, updated_at = @UpdatedAt, updated_by = @UpdatedBy WHERE id = @Id", equipment) > 0;

    public Task<CalibrationEquipment?> GetEquipmentEntityByIdAsync(long equipmentId)
        => QuerySingleOrDefaultAsync("SELECT id, equipment_name, control_no, serial_no, brand, model, location, section_id, pic_id, pic_code, pic_full_name, calib_interval_months, last_calib_date, last_calib_month, next_calib_date, next_calib_month, calib_type, equipment_status, remarks, created_at, updated_at, created_by, updated_by FROM dbo.qa_calib_equipments WHERE id = @EquipmentId", new { EquipmentId = equipmentId });

    public async Task<BulkActionResultDto> BulkUpdateEquipmentStatusAsync(IEnumerable<long> equipmentIds, string status, string? remarks, string updatedBy)
    {
        var results = new List<BulkActionItemResultDto>();
        using var connection = CreateConnection();
        foreach (var item in equipmentIds)
        {
            var row = await connection.QuerySingleOrDefaultAsync<(long Id, string ControlNo)>("SELECT id, control_no FROM dbo.qa_calib_equipments WHERE id = @Id", new { Id = item });
            if (row == default)
            {
                results.Add(new BulkActionItemResultDto(item, string.Empty, false, "Equipment not found."));
                continue;
            }

            await connection.ExecuteAsync("UPDATE dbo.qa_calib_equipments SET equipment_status = @Status, remarks = COALESCE(@Remarks, remarks), updated_at = sysutcdatetime(), updated_by = @UpdatedBy WHERE id = @Id", new { Id = item, Status = status, Remarks = remarks, UpdatedBy = updatedBy });
            results.Add(new BulkActionItemResultDto(item, row.ControlNo, true, "Updated."));
        }

        return BuildBulkResult(equipmentIds.Count(), results);
    }

    public async Task<BulkActionResultDto> BulkMoveEquipmentsAsync(IEnumerable<long> equipmentIds, long sectionId, string updatedBy)
    {
        var results = new List<BulkActionItemResultDto>();
        using var connection = CreateConnection();
        foreach (var item in equipmentIds)
        {
            var row = await connection.QuerySingleOrDefaultAsync<(long Id, string ControlNo)>("SELECT id, control_no FROM dbo.qa_calib_equipments WHERE id = @Id", new { Id = item });
            if (row == default)
            {
                results.Add(new BulkActionItemResultDto(item, string.Empty, false, "Equipment not found."));
                continue;
            }

            await connection.ExecuteAsync("UPDATE dbo.qa_calib_equipments SET section_id = @SectionId, updated_at = sysutcdatetime(), updated_by = @UpdatedBy WHERE id = @Id", new { Id = item, SectionId = sectionId, UpdatedBy = updatedBy });
            results.Add(new BulkActionItemResultDto(item, row.ControlNo, true, "Updated."));
        }

        return BuildBulkResult(equipmentIds.Count(), results);
    }

    public async Task<BulkActionResultDto> BulkDeleteEquipmentsAsync(IEnumerable<long> equipmentIds)
    {
        var results = new List<BulkActionItemResultDto>();
        using var connection = CreateConnection();
        foreach (var item in equipmentIds)
        {
            var equipment = await connection.QuerySingleOrDefaultAsync<(long Id, string ControlNo)>("SELECT id, control_no FROM dbo.qa_calib_equipments WHERE id = @Id", new { Id = item });
            if (equipment == default)
            {
                results.Add(new BulkActionItemResultDto(item, string.Empty, false, "Equipment not found."));
                continue;
            }

            var historyCount = await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)
                FROM dbo.qa_calib_item_details d
                WHERE d.equipment_id = @EquipmentId
                """,
                new { EquipmentId = item });

            if (historyCount > 0)
            {
                results.Add(new BulkActionItemResultDto(item, equipment.ControlNo, false, "Deletion blocked because calibration history exists."));
                continue;
            }

            await connection.ExecuteAsync("DELETE FROM dbo.qa_calib_equipments WHERE id = @Id", new { Id = item });
            results.Add(new BulkActionItemResultDto(item, equipment.ControlNo, true, "Deleted."));
        }

        return BuildBulkResult(equipmentIds.Count(), results);
    }

    public async Task<(IEnumerable<ReminderItemDto> Items, int TotalCount)> GetRemindersAsync(ReminderFilterParams filters)
    {
        using var connection = CreateConnection();
        var items = (await GetReminderItemsAsync(connection, filters)).ToList();
        return (items.Skip(filters.Offset).Take(filters.PageSize), items.Count);
    }

    private static BulkActionResultDto BuildBulkResult(int requestedCount, List<BulkActionItemResultDto> results)
        => new(requestedCount, results.Count(x => x.Success), results.Count(x => !x.Success), results);

    private static string BuildDueCategorySql(string dueCategory)
        => dueCategory switch
        {
            "Due This Month" => "e.next_calib_date >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) AND e.next_calib_date < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))",
            "Next Month" => "e.next_calib_date >= DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) AND e.next_calib_date < DATEADD(MONTH, 2, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))",
            "Overdue" => "e.next_calib_date < DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)",
            _ => "1 = 1"
        };

    private static async Task<IEnumerable<ReminderItemDto>> GetReminderItemsAsync(IDbConnection connection, ReminderFilterParams filters)
    {
        var items = await connection.QueryAsync<ReminderItemDto>(
            """
            SELECT
                e.id AS equipment_id,
                e.equipment_name,
                e.control_no,
                s.section_name,
                e.pic_code,
                e.pic_full_name,
                e.location,
                e.last_calib_date,
                e.next_calib_date,
                e.calib_type,
                CASE
                    WHEN e.next_calib_date < DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) THEN 'Overdue'
                    WHEN e.next_calib_date < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) THEN 'Due This Month'
                    WHEN e.next_calib_date < DATEADD(MONTH, 2, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) THEN 'Next Month'
                    ELSE 'Later'
                END AS due_category,
                CASE WHEN e.next_calib_date < DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS is_overdue
            FROM dbo.qa_calib_equipments e
            INNER JOIN dbo.sections s ON s.section_id = e.section_id
            WHERE (@Search IS NULL OR e.control_no LIKE @LikeSearch OR e.equipment_name LIKE @LikeSearch OR e.pic_full_name LIKE @LikeSearch)
              AND (@SectionId IS NULL OR e.section_id = @SectionId)
              AND (@CalibType IS NULL OR e.calib_type = @CalibType)
              AND (@Location IS NULL OR e.location LIKE @LikeLocation)
            ORDER BY e.next_calib_date, e.equipment_name
            """,
            new
            {
                Search = filters.Search,
                LikeSearch = $"%{filters.Search?.Trim()}%",
                filters.SectionId,
                filters.CalibType,
                filters.Location,
                LikeLocation = $"%{filters.Location?.Trim()}%"
            });

        return items.Where(x => string.IsNullOrWhiteSpace(filters.DueCategory) || string.Equals(x.DueCategory, filters.DueCategory, StringComparison.OrdinalIgnoreCase));
    }
}
