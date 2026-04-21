/*
 ============================================================================
 SEED SCRIPT FOR QA_CALIB DATABASE
 ============================================================================
 Prerequisite:
 1. `shared.sql` has been executed successfully.
 2. `newschema.sql` has been executed successfully.
*/

USE qa_calib;
GO

SET NOCOUNT ON;
GO

-- =========================================================================
-- 1. SECTIONS (both main and backup)
-- =========================================================================
INSERT INTO dbo.sections (section_code, section_name) VALUES
('000', 'JAPANESE'),
('110', 'GENERAL AFFAIR'),
('120', 'HUMAN RESOURCES'),
('130', 'TRAINING CENTER'),
('140', 'SUBSIDIARY'),
('200', 'INFORMATION SYSTEM'),
('310', 'MATERIAL CONTROL'),
('330', 'LOGISTIC CONTROL'),
('350', 'PRODUCTION CONTROL'),
('410', 'CUTTING & CRIMPING'),
('420', 'QUALITY CONTROL'),
('430', 'PROCESS ENGINEERING'),
('450', 'ASSEMBLY'),
('460', 'TRAINING ASSY & CC'),
('510', 'PRODUCTION ENGINEERING'),
('520', 'MAINTENANCE'),
('530', 'DOCUMENT CONTROL'),
('550', 'QUALITY ASSURANCE'),
('600', 'FINANCE & ACCOUNTING'),
('610', 'FINANCE'),
('620', 'ACCOUNTING'),
('630', 'PURCHASING'),
('700', 'SAFETY & MTA BUILDING'),
('800', 'PURCHASING'),
('910', 'ELECTRICAL APPLIANCES W/H'),
('930', 'DESIGN');

INSERT INTO dbo.sections_bkp (section_code, section_name)
SELECT section_code, section_name FROM dbo.sections;
GO

-- =========================================================================
-- 2. POSITIONS (both main and backup)
-- =========================================================================
INSERT INTO dbo.positions (position_code, position_name) VALUES
('010', 'PRESIDENT DIRECTOR'),
('020', 'DIRECTOR'),
('025', 'SEN. GENERAL MANAGER'),
('030', 'GENERAL MANAGER'),
('040', 'DEPUTY GENERAL MANAGER'),
('050', 'ASST. GENERAL MANAGER'),
('055', 'SENIOR MANAGER'),
('060', 'MANAGER'),
('070', 'DEPUTY MANAGER'),
('080', 'ASST. MANAGER'),
('081', 'EXECUTIVE SECRETARY'),
('082', 'INTERPRETER II'),
('090', 'SENIOR SUPERVISOR'),
('091', 'ENGINEER'),
('092', 'SEN. SYSTEM ENGINEER'),
('093', 'SENIOR OFFICER'),
('094', 'INTERPRETER I'),
('100', 'SUPERVISOR'),
('101', 'ASST. ENGINEER'),
('102', 'SYSTEM ENGINEER'),
('103', 'OFFICER'),
('104', 'INTERPRETER'),
('110', 'FOREMAN'),
('111', 'ASST. SYSTEM ENGINEER'),
('112', 'ASST.OFFICER'),
('120', 'SENIOR LEADER'),
('121', 'SENIOR TECHNICIAN'),
('123', 'SENIOR CLERK'),
('130', 'LEADER'),
('131', 'TECHNICIAN'),
('132', 'CLERK'),
('140', 'SUB LEADER'),
('141', 'JUNIOR TECHINICIAN'),
('142', 'JUNIOR CLERK'),
('150', 'OPERATOR'),
('151', 'TECHNICAL OPERATOR'),
('152', 'ADM. OPERATOR'),
('153', 'OPERATOR DC'),
('160', 'SECURITY'),
('170', 'DRIVER'),
('180', 'OFFICE BOY / GIRL'),
('190', 'SENIOR NURSE'),
('191', 'NURSE'),
('192', 'JUNIOR NURSE'),
('200', 'TRAINEE');

INSERT INTO dbo.positions_bkp (position_code, position_name)
SELECT position_code, position_name FROM dbo.positions;
GO

-- =========================================================================
-- 3. LOCATIONS
-- =========================================================================
INSERT INTO dbo.locations (location_name) VALUES
('SBI Plant 1 - Floor 1'),
('SBI Plant 1 - Floor 2'),
('SBI Plant 1 - Floor 3'),
('SBI Plant 2 - Floor 1'),
('SBI Plant 2 - Floor 2'),
('SBI Plant 2 - Floor 3'),
('SBI Plant 3 - Floor 1'),
('SBI Plant 3 - Floor 2'),
('SBI Plant 3 - Floor 3'),
('With External or Vendor');

DECLARE @sectionAbbrev TABLE (section_code NVARCHAR(50), abbrev NVARCHAR(50));

INSERT INTO @sectionAbbrev (section_code, abbrev) VALUES
('000', 'JPN'), ('110', 'GA'), ('120', 'HR'),
('130', 'TC'), ('140', 'SUB'), ('200', 'IS'),
('310', 'MC'), ('330', 'LC'), ('350', 'PC'),
('410', 'C&C'), ('420', 'QC'), ('430', 'PE'),
('450', 'ASSY'), ('460', 'TA&CC'), ('510', 'ProdEng'),
('520', 'MAINT'), ('530', 'DC'), ('550', 'QA'),
('600', 'F&A'), ('610', 'FIN'), ('620', 'ACC'),
('630', 'PUR'), ('700', 'S&MB'), ('800', 'PUR'),
('910', 'EAWH'), ('930', 'DSGN');

INSERT INTO dbo.locations (location_name)
SELECT CONCAT('In Section ', abbrev, ' Room')
FROM dbo.sections s
INNER JOIN @sectionAbbrev a ON s.section_code = a.section_code;
GO

-- =========================================================================
-- 4. USERS
--    Assumes Shared.dbo.employees has been seeded by `shared.sql`.
-- =========================================================================
INSERT INTO dbo.users (employee_id, username, password_hash, email, role, is_active, must_change_password)
SELECT
    e.employee_id,
    e.employee_code,
    CONVERT(NVARCHAR(500), HASHBYTES('SHA2_256', 'P@ssw0rd'), 2),
    CONCAT(e.employee_code, '@example.com'),
    'Employee',
    1,
    1
FROM Shared.dbo.employees e
WHERE e.employee_code IN ('220021', '222299', '223549', '240127');
GO

-- =========================================================================
-- 5. CALIBRATION APPROVERS
-- =========================================================================
INSERT INTO dbo.qa_calib_approvers (employee_id, step_no, is_active, created_by)
SELECT
    e.employee_id,
    CASE e.employee_code
        WHEN '220021' THEN '1'
        WHEN '222299' THEN '2'
        WHEN '223549' THEN '3'
    END,
    1,
    '220021'
FROM Shared.dbo.employees e
WHERE e.employee_code IN ('220021', '222299', '223549');
GO

PRINT 'Database seeding completed successfully.';
