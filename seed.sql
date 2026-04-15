/*
    Minimal seed data for the QA Calibration starter.
    Run this after newschema.sql.
*/

SET NOCOUNT ON;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.sections
    WHERE section_code = N'QA'
)
BEGIN
    INSERT INTO dbo.sections (section_code, section_name, is_active, created_at)
    VALUES (N'QA', N'Quality Assurance', 1, sysutcdatetime());
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.positions
    WHERE position_code = N'QAENG'
)
BEGIN
    INSERT INTO dbo.positions (position_code, position_name, is_active, created_at)
    VALUES (N'QAENG', N'QA Engineer', 1, sysutcdatetime());
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.locations
    WHERE location_name = N'QA Lab'
)
BEGIN
    INSERT INTO dbo.locations (location_name, is_active, created_at)
    VALUES (N'QA Lab', 1, sysutcdatetime());
END;
GO

DECLARE @section_id INT = (
    SELECT TOP 1 section_id
    FROM dbo.sections
    WHERE section_code = N'QA'
);

DECLARE @position_id INT = (
    SELECT TOP 1 position_id
    FROM dbo.positions
    WHERE position_code = N'QAENG'
);

IF NOT EXISTS (
    SELECT 1
    FROM dbo.employees
    WHERE employee_code = N'000001'
)
BEGIN
    INSERT INTO dbo.employees (
        employee_code,
        first_name,
        last_name,
        full_name,
        email,
        section_id,
        position_id,
        employment_status,
        is_active,
        created_at
    )
    VALUES (
        N'000001',
        N'System',
        N'Admin',
        N'System Admin',
        N'admin@qa-calib.local',
        @section_id,
        @position_id,
        N'Active',
        1,
        sysutcdatetime()
    );
END;
GO
