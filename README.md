# QA Calibration Starter

Starter backend API for a QA Calibration System built with .NET 9, Dapper, SQL Server, JWT auth, and the same layered structure as the source project.

## What this starter keeps

- `Project.Api`, `Project.Application`, `Project.Domain`, `Project.Infrastructure`
- JWT auth with login, employee self-registration, password change, password reset, refresh token
- Common API response envelope and global exception middleware
- SQL Server access through `IDbConnectionFactory` and Dapper snake_case mapping
- Auto-open browser behavior in development

## What was removed

- Koperasi-specific modules such as loans, savings, inventory, sales, and reports
- Tenant/member-based auth and role-table logic
- Approval notification and cron dispatch configuration from the previous app

## Schema basis

The starter is aligned to `newschema.sql`, especially:

- `dbo.users`
- `dbo.employees`
- `dbo.password_reset_tokens`
- `dbo.qa_calib_*` tables

## Local setup

1. Run `newschema.sql` against `localhost\\SQLEXPRESS`.
2. Optionally run `seed.sql` for minimal lookup data and a sample employee.
3. Update `Project.Api/appsettings.Development.json` if you need a different database or JWT settings.
4. Start the API with `dotnet run --project Project.Api/Project.Api.csproj`.

## Notes

- Development config keeps hardcoded local defaults by design.
- The first admin user can be created manually in the database or from a future bootstrap/admin workflow.
- `register-employee` creates a default `user` role account for an employee record that does not yet have a user account.
