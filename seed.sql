/*
    =====================================================
    COMPREHENSIVE SEED DATA FOR QA CALIBRATION SYSTEM
    =====================================================
    Run this AFTER newschema.sql (and optional minimal seed.sql).
    All inserts are safe to run multiple times.
*/

SET NOCOUNT ON;
GO

-- =====================================================
-- 1. MASTER TABLES (sections, positions, locations)
-- =====================================================

-- Sections
MERGE INTO dbo.sections AS target
USING (
    VALUES 
        (N'QA', N'Quality Assurance'),
        (N'ENG', N'Engineering'),
        (N'PROD', N'Production'),
        (N'LAB', N'Laboratory')
) AS source(section_code, section_name)
ON target.section_code = source.section_code
WHEN NOT MATCHED THEN
    INSERT (section_code, section_name, is_active, created_at)
    VALUES (source.section_code, source.section_name, 1, sysutcdatetime());

-- Positions
MERGE INTO dbo.positions AS target
USING (
    VALUES 
        (N'QAENG', N'QA Engineer'),
        (N'TECH', N'Technician'),
        (N'SUPV', N'Supervisor'),
        (N'MGR', N'Manager'),
        (N'ADMIN', N'System Administrator')
) AS source(position_code, position_name)
ON target.position_code = source.position_code
WHEN NOT MATCHED THEN
    INSERT (position_code, position_name, is_active, created_at)
    VALUES (source.position_code, source.position_name, 1, sysutcdatetime());

-- Locations
MERGE INTO dbo.locations AS target
USING (
    VALUES 
        (N'QA Lab'),
        (N'Production Floor'),
        (N'Storage Room A'),
        (N'External Calibration Vendor')
) AS source(location_name)
ON target.location_name = source.location_name
WHEN NOT MATCHED THEN
    INSERT (location_name, is_active, created_at)
    VALUES (source.location_name, 1, sysutcdatetime());

-- =====================================================
-- 2. EMPLOYEES & USERS
-- =====================================================

DECLARE @qa_section_id INT = (SELECT section_id FROM dbo.sections WHERE section_code = N'QA');
DECLARE @eng_section_id INT = (SELECT section_id FROM dbo.sections WHERE section_code = N'ENG');
DECLARE @prod_section_id INT = (SELECT section_id FROM dbo.sections WHERE section_code = N'PROD');
DECLARE @lab_section_id INT = (SELECT section_id FROM dbo.sections WHERE section_code = N'LAB');

DECLARE @qaeng_pos_id INT = (SELECT position_id FROM dbo.positions WHERE position_code = N'QAENG');
DECLARE @tech_pos_id INT = (SELECT position_id FROM dbo.positions WHERE position_code = N'TECH');
DECLARE @supv_pos_id INT = (SELECT position_id FROM dbo.positions WHERE position_code = N'SUPV');
DECLARE @mgr_pos_id INT = (SELECT position_id FROM dbo.positions WHERE position_code = N'MGR');
DECLARE @admin_pos_id INT = (SELECT position_id FROM dbo.positions WHERE position_code = N'ADMIN');

-- Insert employees (manager_id will be updated after insert)
CREATE TABLE #emp_ids (employee_code NVARCHAR(6), employee_id INT);

INSERT INTO dbo.employees (
    employee_code, first_name, last_name, full_name, email,
    section_id, position_id, employment_status, is_active, created_at
)
OUTPUT inserted.employee_code, inserted.employee_id INTO #emp_ids
VALUES 
    (N'000001', N'System', N'Admin', N'System Admin', N'admin@qa-calib.local', @qa_section_id, @admin_pos_id, N'Active', 1, sysutcdatetime()),
    (N'EMP001', N'John', N'Smith', N'John Smith', N'john.smith@qa-calib.local', @qa_section_id, @mgr_pos_id, N'Active', 1, sysutcdatetime()),
    (N'EMP002', N'Alice', N'Johnson', N'Alice Johnson', N'alice.johnson@qa-calib.local', @qa_section_id, @supv_pos_id, N'Active', 1, sysutcdatetime()),
    (N'EMP003', N'Bob', N'Williams', N'Bob Williams', N'bob.williams@qa-calib.local', @qa_section_id, @qaeng_pos_id, N'Active', 1, sysutcdatetime()),
    (N'EMP004', N'Carol', N'Brown', N'Carol Brown', N'carol.brown@qa-calib.local', @qa_section_id, @tech_pos_id, N'Active', 1, sysutcdatetime()),
    (N'EMP005', N'David', N'Jones', N'David Jones', N'david.jones@eng.qa-calib.local', @eng_section_id, @tech_pos_id, N'Active', 1, sysutcdatetime());

-- Update manager_id (self-reference)
UPDATE e SET e.manager_id = (SELECT employee_id FROM #emp_ids WHERE employee_code = N'EMP001')
FROM dbo.employees e WHERE e.employee_code IN (N'EMP002', N'EMP003', N'EMP004', N'EMP005');

UPDATE e SET e.manager_id = NULL FROM dbo.employees e WHERE e.employee_code = N'000001'; -- admin has no manager

-- Users (password hash is SHA2_256 of 'P@ssw0rd' – change to your app's hashing method in production)
INSERT INTO dbo.users (employee_id, username, password_hash, email, role, is_active, must_change_password, created_at)
SELECT 
    emp.employee_id,
    emp.employee_code,
    CONVERT(NVARCHAR(500), HASHBYTES('SHA2_256', 'P@ssw0rd'), 2),
    emp.email,
    CASE 
        WHEN emp.employee_code = N'000001' THEN N'Admin'
        WHEN emp.position_id = @mgr_pos_id THEN N'Manager'
        WHEN emp.position_id = @supv_pos_id THEN N'Supervisor'
        ELSE N'User'
    END,
    1, 0, sysutcdatetime()
FROM dbo.employees emp
WHERE NOT EXISTS (SELECT 1 FROM dbo.users u WHERE u.employee_id = emp.employee_id);

DROP TABLE #emp_ids;

-- =====================================================
-- 3. QA CALIBRATION EQUIPMENTS
-- =====================================================

-- Helper: get PIC employee details (use Alice Johnson as PIC for most)
DECLARE @pic_emp_code NVARCHAR(6) = N'EMP002';
DECLARE @pic_id INT = (SELECT employee_id FROM dbo.employees WHERE employee_code = @pic_emp_code);
DECLARE @pic_fullname NVARCHAR(200) = (SELECT full_name FROM dbo.employees WHERE employee_code = @pic_emp_code);

MERGE INTO dbo.qa_calib_equipments AS target
USING (
    VALUES
        (N'Digital Caliper', N'DC-001', N'SN12345', N'Mitutoyo', N'500-196', N'QA Lab', @qa_section_id, 12, '2024-01-15', 'I', 'A', N'High precision caliper', @pic_emp_code, @pic_fullname),
        (N'Micrometer', N'MC-002', N'SN67890', N'Mitutoyo', N'293-240', N'QA Lab', @qa_section_id, 12, '2023-11-10', 'I', 'A', N'External micrometer', @pic_emp_code, @pic_fullname),
        (N'Multimeter', N'MM-003', N'SN11223', N'Fluke', N'87V', N'QA Lab', @qa_section_id, 6, '2024-03-20', 'E', 'A', N'True RMS multimeter', @pic_emp_code, @pic_fullname),
        (N'Thermohygrometer', N'TH-004', N'SN44556', N'Testo', N'608-H1', N'Storage Room A', @lab_section_id, 12, '2023-09-05', 'I', 'A', N'Temp/humidity logger', @pic_emp_code, @pic_fullname),
        (N'Pressure Gauge', N'PG-005', N'SN77889', N'Wika', N'213.40', N'Production Floor', @prod_section_id, 6, '2024-02-28', 'E', 'A', N'Stainless steel gauge', @pic_emp_code, @pic_fullname),
        (N'Vernier Caliper', N'VC-006', N'SN99001', N'Starrett', N'1236', N'QA Lab', @qa_section_id, 12, '2023-12-12', 'I', 'A', N'Standard caliper', @pic_emp_code, @pic_fullname),
        (N'Weighing Scale', N'WS-007', N'SN22334', N'Mettler Toledo', N'ICS425', N'Production Floor', @prod_section_id, 3, '2024-04-01', 'E', 'A', N'Floor scale', @pic_emp_code, @pic_fullname)
) AS source (
    equipment_name, control_no, serial_no, brand, model, location, section_id, calib_interval_months, last_calib_date,
    calib_type, equipment_status, remarks, pic_code, pic_full_name
)
ON target.control_no = source.control_no
WHEN NOT MATCHED THEN
    INSERT (
        equipment_name, control_no, serial_no, brand, model, location, section_id, calib_interval_months,
        last_calib_date, calib_type, equipment_status, remarks, created_by, pic_id, pic_code, pic_full_name
    )
    VALUES (
        source.equipment_name, source.control_no, source.serial_no, source.brand, source.model, source.location,
        source.section_id, source.calib_interval_months, source.last_calib_date, source.calib_type, source.equipment_status,
        source.remarks, @pic_emp_code, @pic_id, source.pic_code, source.pic_full_name
    );

-- =====================================================
-- 4. QA CALIBRATION APPROVERS
-- =====================================================

-- Step 1: Prepared (use Bob Williams - QA Engineer)
-- Step 2: Checked (use Alice Johnson - Supervisor)
-- Step 3: Approved (use John Smith - Manager)

INSERT INTO dbo.qa_calib_approvers (employee_id, step_no, is_active, created_by)
SELECT 
    e.employee_id, step_no, 1, N'000001'
FROM (
    VALUES 
        ((SELECT employee_id FROM dbo.employees WHERE employee_code = N'EMP003'), '1'),
        ((SELECT employee_id FROM dbo.employees WHERE employee_code = N'EMP002'), '2'),
        ((SELECT employee_id FROM dbo.employees WHERE employee_code = N'EMP001'), '3')
) AS data(emp_id, step_no)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.qa_calib_approvers a 
    WHERE a.employee_id = data.emp_id AND a.step_no = data.step_no
);

-- =====================================================
-- 5. CALIBRATION HEADERS (Plan & Actual for current month)
-- =====================================================

DECLARE @current_year INT = YEAR(sysutcdatetime());
DECLARE @current_month INT = MONTH(sysutcdatetime());

-- Plan Header (calib_phase = 'P')
INSERT INTO dbo.qa_calib_main_headers (calib_no, calib_phase, calib_type, calib_month, calib_year, remarks, created_by)
SELECT 
    N'CALIB-P-' + CAST(@current_year AS NVARCHAR) + N'-' + RIGHT('0' + CAST(@current_month AS NVARCHAR),2) + N'-001',
    'P', 'I', @current_month, @current_year, N'Monthly internal calibration plan', N'000001'
WHERE NOT EXISTS (SELECT 1 FROM dbo.qa_calib_main_headers WHERE calib_no = N'CALIB-P-' + CAST(@current_year AS NVARCHAR) + N'-' + RIGHT('0' + CAST(@current_month AS NVARCHAR),2) + N'-001');

DECLARE @plan_header_id INT = (SELECT id FROM dbo.qa_calib_main_headers WHERE calib_no = N'CALIB-P-' + CAST(@current_year AS NVARCHAR) + N'-' + RIGHT('0' + CAST(@current_month AS NVARCHAR),2) + N'-001');

-- Actual Header (calib_phase = 'A') – usually created later, but we seed one completed example from previous month
DECLARE @prev_month INT = @current_month - 1;
IF @prev_month = 0 SET @prev_month = 12;

INSERT INTO dbo.qa_calib_main_headers (calib_no, calib_phase, calib_type, calib_month, calib_year, remarks, created_by)
SELECT 
    N'CALIB-A-' + CAST(@current_year AS NVARCHAR) + N'-' + RIGHT('0' + CAST(@prev_month AS NVARCHAR),2) + N'-001',
    'A', 'I', @prev_month, @current_year, N'Completed internal calibration for previous month', N'000001'
WHERE NOT EXISTS (SELECT 1 FROM dbo.qa_calib_main_headers WHERE calib_no = N'CALIB-A-' + CAST(@current_year AS NVARCHAR) + N'-' + RIGHT('0' + CAST(@prev_month AS NVARCHAR),2) + N'-001');

DECLARE @actual_header_id INT = (SELECT id FROM dbo.qa_calib_main_headers WHERE calib_no = N'CALIB-A-' + CAST(@current_year AS NVARCHAR) + N'-' + RIGHT('0' + CAST(@prev_month AS NVARCHAR),2) + N'-001');

-- =====================================================
-- 6. CALIBRATION PLAN & ACTUAL RECORDS
-- =====================================================

-- Plan record (status = 'D' draft, we'll keep as draft)
INSERT INTO dbo.qa_calib_plans (header_id, calib_status, created_by)
SELECT @plan_header_id, 'D', N'000001'
WHERE NOT EXISTS (SELECT 1 FROM dbo.qa_calib_plans WHERE header_id = @plan_header_id);

-- Actual record (status = 'X' completed, with completion info)
INSERT INTO dbo.qa_calib_actuals (header_id, calib_status, completed_dt, completed_by)
SELECT @actual_header_id, 'X', DATEADD(day, -5, sysutcdatetime()), N'EMP004'
WHERE NOT EXISTS (SELECT 1 FROM dbo.qa_calib_actuals WHERE header_id = @actual_header_id);

-- =====================================================
-- 7. CALIBRATION ITEMS & DETAILS (for the Actual header)
-- =====================================================

-- Items: group equipment by equipment_name (for this example, we add two items)
INSERT INTO dbo.qa_calib_items (header_id, equipment_name, item_count, item_completed, std_used, remarks, created_by)
SELECT @actual_header_id, N'Digital Caliper', 1, 1, N'Gauge block set #101', N'All OK', N'EMP004'
WHERE NOT EXISTS (SELECT 1 FROM dbo.qa_calib_items WHERE header_id = @actual_header_id AND equipment_name = N'Digital Caliper');

INSERT INTO dbo.qa_calib_items (header_id, equipment_name, item_count, item_completed, std_used, remarks, created_by)
SELECT @actual_header_id, N'Micrometer', 1, 1, N'Standard micrometer set', N'Passed', N'EMP004'
WHERE NOT EXISTS (SELECT 1 FROM dbo.qa_calib_items WHERE header_id = @actual_header_id AND equipment_name = N'Micrometer');

-- Get item IDs
DECLARE @item1_id INT = (SELECT id FROM dbo.qa_calib_items WHERE header_id = @actual_header_id AND equipment_name = N'Digital Caliper');
DECLARE @item2_id INT = (SELECT id FROM dbo.qa_calib_items WHERE header_id = @actual_header_id AND equipment_name = N'Micrometer');

-- Equipment IDs for the two items
DECLARE @eq_caliper_id INT = (SELECT id FROM dbo.qa_calib_equipments WHERE control_no = N'DC-001');
DECLARE @eq_micrometer_id INT = (SELECT id FROM dbo.qa_calib_equipments WHERE control_no = N'MC-002');

-- Insert details (calibration results)
INSERT INTO dbo.qa_calib_item_details (item_id, equipment_id, calib_result, overdue_flag, certificate_no, remarks, created_by)
SELECT @item1_id, @eq_caliper_id, 'O', 0, NULL, N'Within tolerance', N'EMP004'
WHERE NOT EXISTS (SELECT 1 FROM dbo.qa_calib_item_details WHERE item_id = @item1_id AND equipment_id = @eq_caliper_id);

INSERT INTO dbo.qa_calib_item_details (item_id, equipment_id, calib_result, overdue_flag, certificate_no, remarks, created_by)
SELECT @item2_id, @eq_micrometer_id, 'O', 0, NULL, N'Passed calibration', N'EMP004'
WHERE NOT EXISTS (SELECT 1 FROM dbo.qa_calib_item_details WHERE item_id = @item2_id AND equipment_id = @eq_micrometer_id);

-- =====================================================
-- 8. CALIBRATION WORKERS (for the Actual header)
-- =====================================================

DECLARE @actual_id INT = (SELECT id FROM dbo.qa_calib_actuals WHERE header_id = @actual_header_id);
DECLARE @tech_emp_id INT = (SELECT employee_id FROM dbo.employees WHERE employee_code = N'EMP004');
DECLARE @tech_emp_code NVARCHAR(6) = N'EMP004';
DECLARE @tech_fullname NVARCHAR(200) = (SELECT full_name FROM dbo.employees WHERE employee_code = @tech_emp_code);

INSERT INTO dbo.qa_calib_workers (actual_id, employee_id, employee_code, employee_full_name, is_pic, created_by)
SELECT @actual_id, @tech_emp_id, @tech_emp_code, @tech_fullname, 1, N'EMP004'
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.qa_calib_workers 
    WHERE actual_id = @actual_id AND employee_id = @tech_emp_id
);

-- Optionally add an external worker
INSERT INTO dbo.qa_calib_workers (actual_id, external_party_name, is_pic, created_by)
SELECT @actual_id, N'Ext Calibration Services Ltd.', 0, N'EMP004'
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.qa_calib_workers 
    WHERE actual_id = @actual_id AND external_party_name = N'Ext Calibration Services Ltd.'
);

-- =====================================================
-- 9. CALIBRATION APPROVALS (for the Actual header – completed)
-- =====================================================

-- For step 1 (Prepared) – Bob Williams
INSERT INTO dbo.qa_calib_approvals (header_id, step_no, employee_id, employee_code, employee_full_name, action, remarks, actioned_at, created_by)
SELECT @actual_header_id, '1', e.employee_id, e.employee_code, e.full_name, 'S', N'Prepared and submitted', DATEADD(day, -10, sysutcdatetime()), N'EMP003'
FROM dbo.employees e WHERE e.employee_code = N'EMP003'
AND NOT EXISTS (SELECT 1 FROM dbo.qa_calib_approvals WHERE header_id = @actual_header_id AND step_no = '1');

-- Step 2 (Checked) – Alice Johnson
INSERT INTO dbo.qa_calib_approvals (header_id, step_no, employee_id, employee_code, employee_full_name, action, remarks, actioned_at, created_by)
SELECT @actual_header_id, '2', e.employee_id, e.employee_code, e.full_name, 'S', N'Checked and approved', DATEADD(day, -7, sysutcdatetime()), N'EMP002'
FROM dbo.employees e WHERE e.employee_code = N'EMP002'
AND NOT EXISTS (SELECT 1 FROM dbo.qa_calib_approvals WHERE header_id = @actual_header_id AND step_no = '2');

-- Step 3 (Approved) – John Smith
INSERT INTO dbo.qa_calib_approvals (header_id, step_no, employee_id, employee_code, employee_full_name, action, remarks, actioned_at, created_by)
SELECT @actual_header_id, '3', e.employee_id, e.employee_code, e.full_name, 'S', N'Final approval granted', DATEADD(day, -3, sysutcdatetime()), N'EMP001'
FROM dbo.employees e WHERE e.employee_code = N'EMP001'
AND NOT EXISTS (SELECT 1 FROM dbo.qa_calib_approvals WHERE header_id = @actual_header_id AND step_no = '3');

-- =====================================================
-- 10. (OPTIONAL) Add a few more equipments for future planning
-- =====================================================
MERGE INTO dbo.qa_calib_equipments AS target
USING (
    VALUES
        (N'Infrared Thermometer', N'IRT-008', N'SN55667', N'Fluke', N'62 MAX', N'QA Lab', @qa_section_id, 6, '2024-02-10', 'I', 'A', N'Non-contact thermometer', @pic_emp_code, @pic_fullname),
        (N'pH Meter', N'PH-009', N'SN88990', N'Hanna', N'HI98107', N'Storage Room A', @lab_section_id, 12, '2023-08-22', 'E', 'A', N'Waterproof pH tester', @pic_emp_code, @pic_fullname)
) AS source (
    equipment_name, control_no, serial_no, brand, model, location, section_id, calib_interval_months, last_calib_date,
    calib_type, equipment_status, remarks, pic_code, pic_full_name
)
ON target.control_no = source.control_no
WHEN NOT MATCHED THEN
    INSERT (
        equipment_name, control_no, serial_no, brand, model, location, section_id, calib_interval_months,
        last_calib_date, calib_type, equipment_status, remarks, created_by, pic_id, pic_code, pic_full_name
    )
    VALUES (
        source.equipment_name, source.control_no, source.serial_no, source.brand, source.model, source.location,
        source.section_id, source.calib_interval_months, source.last_calib_date, source.calib_type, source.equipment_status,
        source.remarks, @pic_emp_code, @pic_id, source.pic_code, source.pic_full_name
    );

PRINT 'Seeding completed successfully.';
GO