IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'qa_calib')
BEGIN
    CREATE DATABASE qa_calib;
END;

GO

USE [qa_calib];

GO

-- =========================
-- DROP (safe order)
-- =========================
IF OBJECT_ID('dbo.qa_calib_equipment_details', 'U') IS NOT NULL DROP TABLE dbo.qa_calib_equipment_details;
IF OBJECT_ID('dbo.qa_calib_item_details',  'U') IS NOT NULL DROP TABLE dbo.qa_calib_item_details;
IF OBJECT_ID('dbo.qa_calib_items',         'U') IS NOT NULL DROP TABLE dbo.qa_calib_items;
IF OBJECT_ID('dbo.qa_calib_approvals',     'U') IS NOT NULL DROP TABLE dbo.qa_calib_approvals;
IF OBJECT_ID('dbo.qa_calib_workers',       'U') IS NOT NULL DROP TABLE dbo.qa_calib_workers;
IF OBJECT_ID('dbo.qa_calib_actuals',       'U') IS NOT NULL DROP TABLE dbo.qa_calib_actuals;
IF OBJECT_ID('dbo.qa_calib_plans',         'U') IS NOT NULL DROP TABLE dbo.qa_calib_plans;
IF OBJECT_ID('dbo.qa_calib_main_headers',  'U') IS NOT NULL DROP TABLE dbo.qa_calib_main_headers;
IF OBJECT_ID('dbo.qa_calib_approvers',     'U') IS NOT NULL DROP TABLE dbo.qa_calib_approvers;
IF OBJECT_ID('dbo.qa_calib_equipments',    'U') IS NOT NULL DROP TABLE dbo.qa_calib_equipments;

IF OBJECT_ID('dbo.password_reset_tokens', 'U') IS NOT NULL DROP TABLE dbo.password_reset_tokens;
IF OBJECT_ID('dbo.users', 'U') IS NOT NULL DROP TABLE dbo.users;
IF OBJECT_ID('dbo.sections', 'U') IS NOT NULL DROP TABLE dbo.sections;
IF OBJECT_ID('dbo.positions', 'U') IS NOT NULL DROP TABLE dbo.positions;
IF OBJECT_ID('dbo.locations', 'U') IS NOT NULL DROP TABLE dbo.locations;

IF OBJECT_ID('dbo.sections_bkp', 'U') IS NOT NULL DROP TABLE dbo.sections_bkp;
IF OBJECT_ID('dbo.positions_bkp', 'U') IS NOT NULL DROP TABLE dbo.positions_bkp;
GO


-- =========================
-- SECTIONS
-- =========================
CREATE TABLE dbo.sections (
    section_id      INT IDENTITY PRIMARY KEY,
    section_code    NVARCHAR(50) NOT NULL UNIQUE,
    section_name    NVARCHAR(200) NOT NULL,
    is_active       BIT NOT NULL DEFAULT 1,
    created_at      DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at      DATETIME2 NULL
);
GO


-- =========================
-- POSITIONS
-- =========================
CREATE TABLE dbo.positions (
    position_id      INT IDENTITY PRIMARY KEY,
    position_code    NVARCHAR(50) NOT NULL UNIQUE,
    position_name    NVARCHAR(200) NOT NULL,
    is_active        BIT NOT NULL DEFAULT 1,
    created_at       DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at       DATETIME2 NULL
);
GO


-- =========================
-- LOCATIONS
-- =========================
CREATE TABLE dbo.locations (
    location_id      INT IDENTITY PRIMARY KEY,
    location_name    NVARCHAR(200) NOT NULL,
    is_active        BIT NOT NULL DEFAULT 1,
    created_at       DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at       DATETIME2 NULL
);
GO


-- =========================
-- SECTIONS BACKUP
-- =========================
CREATE TABLE dbo.sections_bkp (
    section_id      INT IDENTITY PRIMARY KEY,
    section_code    NVARCHAR(50) NOT NULL UNIQUE,
    section_name    NVARCHAR(200) NOT NULL,
    is_active       BIT NOT NULL DEFAULT 1,
    created_at      DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at      DATETIME2 NULL
);
GO


-- =========================
-- POSITIONS BACKUP
-- =========================
CREATE TABLE dbo.positions_bkp (
    position_id      INT IDENTITY PRIMARY KEY,
    position_code    NVARCHAR(50) NOT NULL UNIQUE,
    position_name    NVARCHAR(200) NOT NULL,
    is_active        BIT NOT NULL DEFAULT 1,
    created_at       DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at       DATETIME2 NULL
);
GO


-- =========================
-- USERS
-- =========================
CREATE TABLE dbo.users (
    user_id                     INT IDENTITY PRIMARY KEY,
    employee_id                 INT NULL,  -- ref: Shared.dbo.employees.employee_id; cross-database FK intentionally omitted
    username                    NVARCHAR(100) NOT NULL UNIQUE,
    password_hash               NVARCHAR(500) NOT NULL,
    email                       NVARCHAR(200) NOT NULL,
    role                        NVARCHAR(50) NOT NULL,
    is_active                   BIT NOT NULL DEFAULT 1,
    failed_login_attempts       INT NOT NULL DEFAULT 0,
    must_change_password        BIT NOT NULL DEFAULT 1,
    last_login                  DATETIME2 NULL,
    lockout_until               DATETIME2 NULL,
    refresh_token               NVARCHAR(MAX) NULL,
    refresh_token_expires_at    DATETIME2 NULL,
    created_at                  DATETIME2 NOT NULL DEFAULT GETDATE(),  -- user's creation date, defaults to current date when the record is created
    updated_at                  DATETIME2 NULL
);
GO


-- =========================
-- PASSWORD RESET TOKENS
-- =========================
CREATE TABLE dbo.password_reset_tokens (
    id             BIGINT IDENTITY PRIMARY KEY,
    user_id        INT NOT NULL,  -- ref: user id, required to associate the token with a specific user, should reference to the users table i think
    token          NVARCHAR(200) NOT NULL UNIQUE,
    expires_at     DATETIME2 NOT NULL,
    created_at     DATETIME2 NOT NULL DEFAULT GETDATE(),  -- token creation date, defaults to current date when the record is created
    consumed_at    DATETIME2 NULL,

    CONSTRAINT FK_password_reset_tokens_users
        FOREIGN KEY (user_id)
        REFERENCES dbo.users(user_id)
        ON DELETE CASCADE
);

CREATE INDEX IX_password_reset_tokens_user_status
ON dbo.password_reset_tokens(user_id, consumed_at, expires_at);
GO


-- =========================
-- CALIBRATION EQUIPMENTS
-- =========================
CREATE TABLE dbo.qa_calib_equipments (
    id                       INT IDENTITY PRIMARY KEY,  -- equipment's id/identity (primary key with auto-increment)
    equipment_name           NVARCHAR(200) NOT NULL,  -- equipment's name, it will be heavily used across the system — especially in the app level for grouping identical items and others
    control_no               NVARCHAR(100) NOT NULL UNIQUE,  -- equipment's control number, unique identifier for each equipment (e.g., "DC-XX", "EQ-001", "AS/MT/002", etc)
    serial_no                NVARCHAR(100) NULL,  -- equipment's serial number, can be null if not applicable or unknown, basically its serial number
    brand                    NVARCHAR(200) NULL,  -- equipment's brand, can be null if not applicable or unknown, basically its brand
    model                    NVARCHAR(200) NULL,  -- equipment's model, can be null if not applicable or unknown, basically its model
    location                 NVARCHAR(200) NOT NULL,  -- equipment's location, choose from existing locations table data (thru autocomplete) or enter a custom one (e.g., room, area, "Dekat Blabla").
    section_id               INT NOT NULL,  -- ref: section id, cant be null, should reference to the sections table manually, can fallback to its _bkp counterpart if there are any issues with referencing
    pic_id                   INT NOT NULL,  -- ref: Shared.dbo.employees.employee_id
    pic_code                 NVARCHAR(6) NOT NULL,  -- denormalized from Shared.dbo.employees.employee_code
    pic_full_name            NVARCHAR(200) NOT NULL,  -- denormalized from Shared.dbo.employees.full_name
    calib_interval_months    INT NOT NULL,  -- calibration interval in months (1 = monthly, 3 = every 3 months, 12 = yearly, 20 = every 20 months, etc.)
    last_calib_date          DATE NOT NULL,  -- equipment's last calibration date, required due to existing manual records, if no last calibration date or for new stuff, use GETDATE()
    last_calib_month         AS MONTH(last_calib_date) PERSISTED,  -- equipment's last calibration month, same like last_calib_date but instead just get the month
    next_calib_date          AS DATEADD(MONTH, calib_interval_months, last_calib_date) PERSISTED,  -- equipment's next calibration date, computed automatically from last_calib_date + calib_interval_months
    next_calib_month         AS MONTH(DATEADD(MONTH, calib_interval_months, last_calib_date)) PERSISTED,  -- equipment's next calibration month, same like next_calib_date but instead just get the month
    calib_type               CHAR(1) NOT NULL DEFAULT 'I',  -- calibration type maps to calibration type enum ('I' = internal, 'E' = external) in project.domain.enums, to indicate whether the equipment calibrated internally or externally
    equipment_status         CHAR(1) NOT NULL DEFAULT 'A',  -- equipment's status maps to equipment status enum ('A' = active, 'O' = out for service, 'S' = scrapped) in project.domain.enums
    remarks                  NVARCHAR(MAX) NULL,  -- additional remarks or notes about the equipment, can be null
    created_at               DATETIME2 NOT NULL DEFAULT GETDATE(),  -- equipment's entry date, defaults to current date when the record is created
    updated_at               DATETIME2 NULL,  -- equipment's last update, can be null, should be updated to whenever the record is updated
    created_by               NVARCHAR(6) NOT NULL,  -- equipment's entry by, required to indicate who created the record, record the employee code (e.g., "525025", "220021", etc) of the creator
    updated_by               NVARCHAR(6) NULL,  -- equipment's last update by, can be null, should be updated to whenever the record is updated, record the employee code (e.g., "525025", "220021", etc) of the person who updated the record

    CONSTRAINT CK_equipments_calib_type 
        CHECK (calib_type IN ('I','E')),

    CONSTRAINT CK_equipments_status 
        CHECK (equipment_status IN ('A','O','S'))
);
GO


-- =========================
-- CALIBRATION APPROVERS
-- =========================
CREATE TABLE dbo.qa_calib_approvers (
    id             INT IDENTITY PRIMARY KEY,
    employee_id    INT NOT NULL,  -- ref: Shared.dbo.employees.employee_id
    step_no        CHAR(1) NOT NULL,  -- approver's step_no maps to approval step enum ('1'=Prepared, '2'=Checked, '3'=Approved) in project.domain.enums
    is_active      BIT NOT NULL DEFAULT 1,
    created_at     DATETIME2 NOT NULL DEFAULT GETDATE(),  -- approver creation date, defaults to current date when the record is created
    updated_at     DATETIME2 NULL,
    created_by     NVARCHAR(6) NOT NULL,
    updated_by     NVARCHAR(6) NULL,

    CONSTRAINT CK_approvers_step_no
        CHECK (step_no IN ('1','2','3')),

    CONSTRAINT UQ_approvers_employee_step
        UNIQUE (employee_id, step_no)
);


-- =========================
-- CALIBRATION (HEADER)
-- =========================
CREATE TABLE dbo.qa_calib_main_headers (
    id             INT IDENTITY PRIMARY KEY,
    calib_no       NVARCHAR(100) NOT NULL UNIQUE,  -- calibration's identification number, unique identifier for each calibration record (e.g., "CALIB-2024-001", "CALIB-2024-002", etc)
    calib_phase    CHAR(1) NOT NULL DEFAULT 'P',  -- calibration phase maps to calibration phase enum ('P' = plan, 'A' = actual) in project.domain.enums, to indicate whether the record is for planned calibration or actual calibration
    calib_type     CHAR(1) NOT NULL,  -- calibration type maps to calibration type enum ('I' = internal, 'E' = external) in project.domain.enums, to indicate whether the calibration is planned to be internal or external
    calib_month    INT NOT NULL,  -- calibration month (1-12), required to indicate the month of the calibration, should be between 1 and 12
    calib_year     INT NOT NULL,  -- calibration year (e.g., 2024), required to indicate the year of the calibration
    remarks        NVARCHAR(MAX) NULL,
    created_at     DATETIME2 NOT NULL DEFAULT GETDATE(),  -- approver creation date, defaults to current date when the record is created
    updated_at     DATETIME2 NULL,
    created_by     NVARCHAR(6) NOT NULL,
    updated_by     NVARCHAR(6) NULL,

    CONSTRAINT CK_headers_phase 
        CHECK (calib_phase IN ('P','A')),

    CONSTRAINT CK_plans_calib_type 
        CHECK (calib_type IN ('I','E')),

    CONSTRAINT CK_headers_calib_month
        CHECK (calib_month >= 1 AND calib_month <= 12)
);

-- =========================
-- CALIBRATION PLAN
-- =========================
CREATE TABLE dbo.qa_calib_plans (
    id              INT IDENTITY PRIMARY KEY,
    header_id       INT NOT NULL,  -- ref: calibration main header id, cant be null, should reference to the qa_calib_main_headers table to link the plan to its main header
    calib_status    CHAR(1) NOT NULL DEFAULT 'D',  -- calibration status maps to calibration status enum ('D' = draft, 'S' = submitted for approval, 'L' = locked) in project.domain.enums, to indicate the current status of the calibration plan
    locked_at       DATETIME2 NULL,  -- lock date, can be null, should be updated to the date when the plan is locked (i.e., when the plan status is updated to 'L')
    locked_by       NVARCHAR(6) NULL,  -- lock by, can be null, should be updated to the employee code (e.g., "525025", "220021", etc) of the person who locked the plan when the plan is locked (i.e., when the plan status is updated to 'L')
    
    CONSTRAINT FK_plans_header
        FOREIGN KEY (header_id)
        REFERENCES dbo.qa_calib_main_headers(id)
        ON DELETE CASCADE,

    CONSTRAINT UQ_plans_header UNIQUE (header_id),

    CONSTRAINT CK_plans_status 
        CHECK (calib_status IN ('D','S','L'))
);


-- =========================
-- CALIBRATION ACTUAL
-- =========================
CREATE TABLE dbo.qa_calib_actuals (
    id               INT IDENTITY PRIMARY KEY,
    plan_id          INT NOT NULL,  -- ref: qa_calib_plans.id to enforce a single actual document per plan
    header_id        INT NOT NULL,  -- ref: calibration main header id, cant be null, should reference to the qa_calib_main_headers table to link the actual calibration to its main header
    calib_status     CHAR(1) NOT NULL DEFAULT 'G',  -- calibration status maps to calibration status enum ('G' = ongoing, 'X' = completed) in project.domain.enums, to indicate the current status of the actual calibration
    completed_dt     DATETIME2 NULL,  -- completion date, can be null, should be updated to the date when the actual calibration is completed (i.e., when the actual calibration status is updated to 'X')
    completed_by     NVARCHAR(6) NULL,  -- completion by, can be null, should be updated to the employee code (e.g., "525025", "220021", etc) of the person who completed the actual calibration when the actual calibration is completed (i.e., when the actual calibration status is updated to 'X')
    
    CONSTRAINT FK_actuals_plan
        FOREIGN KEY (plan_id)
        REFERENCES dbo.qa_calib_plans(id),

    CONSTRAINT FK_actuals_header
        FOREIGN KEY (header_id)
        REFERENCES dbo.qa_calib_main_headers(id)
        ON DELETE CASCADE,

    CONSTRAINT UQ_actuals_plan UNIQUE (plan_id),

    CONSTRAINT UQ_actuals_header UNIQUE (header_id),

    CONSTRAINT CK_actuals_status 
        CHECK (calib_status IN ('G','X'))
);


-- =========================
-- CALIBRATION WORKERS
-- =========================
CREATE TABLE dbo.qa_calib_workers (
    id                     INT IDENTITY PRIMARY KEY,
    actual_id              INT NOT NULL,  -- ref: qa_calib_actuals.id — technicians belong to actual execution
    employee_id            INT NULL,  -- ref: Shared.dbo.employees.employee_id
    employee_code          NVARCHAR(6) NULL,  -- denormalized from Shared.dbo.employees.employee_code
    employee_full_name     NVARCHAR(200) NULL,  -- denormalized from Shared.dbo.employees.full_name
    external_party_name    NVARCHAR(200) NULL,  -- name of the external party (not in employees table), can be null if the technician is internal or if the name or company is unknown
    is_pic                 BIT NOT NULL DEFAULT 0,  -- 1 = person-in-charge / lead technician
    created_at             DATETIME2 NOT NULL DEFAULT GETDATE(),
    created_by             NVARCHAR(6) NOT NULL,
 
    CONSTRAINT FK_workers_actual
        FOREIGN KEY (actual_id)
        REFERENCES dbo.qa_calib_actuals(id)
        ON DELETE CASCADE,
 
    -- Prevent duplicate worker entries for the same actual thru app level and selects
    
    CONSTRAINT CK_workers_identity
    CHECK (
        employee_code IS NOT NULL  -- internal worker
        OR external_party_name IS NOT NULL  -- external worker
    )
);
GO


-- =========================
-- CALIBRATION APPROVALS
-- =========================
CREATE TABLE dbo.qa_calib_approvals (
    id                    INT IDENTITY PRIMARY KEY,
    header_id             INT NOT NULL,  -- ref: qa_calib_main_headers.id
    step_no               CHAR(1) NOT NULL,  -- '1' = Prepared, '2' = Checked, '3' = Approved
    employee_id           INT NULL,  -- ref: Shared.dbo.employees.employee_id
    employee_code         NVARCHAR(6) NULL,  -- denormalized from Shared.dbo.employees.employee_code
    employee_full_name    NVARCHAR(200) NULL,  -- denormalized from Shared.dbo.employees.full_name
    action                CHAR(1) NOT NULL DEFAULT 'C',  -- 'C' = Cancel, 'S' = Submit
    remarks               NVARCHAR(500) NULL,
    actioned_at           DATETIME2 NULL,  -- when the approval action was taken
    created_at            DATETIME2 NOT NULL DEFAULT GETDATE(),
    created_by            NVARCHAR(6) NOT NULL,
    updated_at            DATETIME2 NULL,
    updated_by            NVARCHAR(6) NULL,
 
    CONSTRAINT FK_approvals_header
        FOREIGN KEY (header_id)
        REFERENCES dbo.qa_calib_main_headers(id)
        ON DELETE CASCADE,

    CONSTRAINT CK_approvals_step_no
        CHECK (step_no IN ('1','2','3')),
 
    CONSTRAINT CK_approvals_action
        CHECK (action IN ('C','S')),

    CONSTRAINT UQ_approvals_header_step
        UNIQUE (header_id, step_no)
);


-- =========================
-- CALIBRATION ITEMS
-- =========================
CREATE TABLE dbo.qa_calib_items (
    id                INT IDENTITY PRIMARY KEY,
    header_id         INT NOT NULL,  -- ref: qa_calib_main_headers.id
    equipment_name    NVARCHAR(200) NOT NULL,  -- equipment type/group name (e.g., "Stainless Steel Ruler Assy")
    item_count        INT NOT NULL DEFAULT 0,  -- total number of equipment units under this group
    item_completed    INT NOT NULL DEFAULT 0,  -- how many units have been calibrated (for actual phase)
    std_used          NVARCHAR(200) NULL,  -- standard/reference used (internal) or vendor/lab name (external)
    remarks           NVARCHAR(MAX) NULL,
    created_at        DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at        DATETIME2 NULL,
    created_by        NVARCHAR(6) NOT NULL,
    updated_by        NVARCHAR(6) NULL,
 
    CONSTRAINT FK_items_header
        FOREIGN KEY (header_id)
        REFERENCES dbo.qa_calib_main_headers(id)
        ON DELETE CASCADE,

    CONSTRAINT CK_items_count_nonneg
        CHECK (item_count >= 0),
 
    CONSTRAINT CK_items_completed_nonneg
        CHECK (item_completed >= 0)
);


-- =========================
-- CALIBRATION ITEM DETAILS
-- =========================
CREATE TABLE dbo.qa_calib_item_details (
    id              INT IDENTITY PRIMARY KEY,
    item_id         INT NOT NULL,  -- ref: qa_calib_items.id (the group this unit belongs to)
    equipment_id    INT NOT NULL,  -- ref: qa_calib_equipments.id (specific equipment unit)
    calib_result    CHAR(1) NULL,  -- 'O' = OK, 'N' = NG, NULL = not yet done
    overdue_flag    BIT NOT NULL DEFAULT 0,  -- 1 = this equipment was past its next_calib_month when calibrated
    certificate_no  NVARCHAR(100) NULL,  -- external calibration certificate number (null for internal and possibly null/optional for external if not yet available)
    remarks         NVARCHAR(MAX) NULL,
    created_at      DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at      DATETIME2 NULL,
    created_by      NVARCHAR(6) NOT NULL,
    updated_by      NVARCHAR(6) NULL,

    CONSTRAINT FK_details_item
        FOREIGN KEY (item_id)
        REFERENCES dbo.qa_calib_items(id)
        ON DELETE CASCADE,
 
    CONSTRAINT CK_details_result
        CHECK (calib_result IN ('O','N') OR calib_result IS NULL)
);
 
CREATE INDEX IX_details_item
    ON dbo.qa_calib_item_details(item_id);
GO


-- =========================
-- EQUIPMENT DETAILS SNAPSHOT (at time of calibration detail entry)
-- =========================
CREATE TABLE dbo.qa_calib_equipment_details (
    id                       INT IDENTITY PRIMARY KEY,
    detail_id                INT NOT NULL,  -- ref: qa_calib_item_details.id (1:1)
    equipment_id             INT NOT NULL,  -- original equipment id
    equipment_name           NVARCHAR(200) NOT NULL,
    control_no               NVARCHAR(100) NOT NULL,
    serial_no                NVARCHAR(100) NULL,
    brand                    NVARCHAR(200) NULL,
    model                    NVARCHAR(200) NULL,
    location                 NVARCHAR(200) NOT NULL,
    section_code             NVARCHAR(50) NOT NULL,
    section_name             NVARCHAR(200) NOT NULL,
    calib_interval_months    INT NOT NULL,
    last_calib_date          DATE NOT NULL,
    last_calib_month         INT NOT NULL,
    next_calib_date          DATE NOT NULL,  -- computed value at snapshot time
    next_calib_month         INT NOT NULL,  -- computed value at snapshot time
    pic_code                 NVARCHAR(6) NOT NULL,
    pic_full_name            NVARCHAR(200) NOT NULL,

    CONSTRAINT FK_equipmentsnapshots_detail
        FOREIGN KEY (detail_id)
        REFERENCES dbo.qa_calib_item_details(id)
        ON DELETE CASCADE,

    CONSTRAINT UQ_equipmentsnapshots_detail UNIQUE (detail_id)
);
