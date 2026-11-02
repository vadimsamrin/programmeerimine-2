# Project management

ASP.NET Core 8 project management system for the Programmeerimine 2 assignment.

## Solution projects

- `KooliProjekt.Application` - data model, EF Core context, migrations, repository and MediatR handlers.
- `KooliProjekt.WebApi` - authenticated REST API, file uploads and automatic seed data.
- `KooliProjekt.Application.UnitTests` - handler unit tests.
- `KooliProjekt.IntegrationTests` - API integration tests that run against an in-memory database.
- `KooliProjekt.ApiClient` - shared HTTP API client.
- `KooliProjekt.WindowsForms` - desktop CRUD client implemented with MVP.
- `KooliProjekt.WindowsForms.UnitTests` - presenter unit tests with a fake API client.
- `KooliProjekt.WpfApplication` - WPF CRUD client implemented with ViewModel commands.
- `KooliProjekt.WpfApplication.UnitTests` - ViewModel unit tests.
- `KooliProjekt.BlazorWASM` - Blazor WebAssembly project list and edit form.
- `KooliProjektAngular` - optional Angular client.

## Database setup

The project uses SQL Server LocalDB by default:

```powershell
dotnet tool run dotnet-ef database update --project KooliProjekt.Application --startup-project KooliProjekt.WebApi
dotnet run --project KooliProjekt.WebApi --launch-profile http
```

If `LocalDB` is not installed, add the **SQL Server Express LocalDB** component in Visual Studio Installer. The migration is already in `KooliProjekt.Application/Migrations`.

The first launch creates ten projects, ten tasks, ten work logs, ten attachments and a development login:

- email: `demo@project.local`
- password: `Project123!`

For a real user, call `POST /register` and then `POST /login?useCookies=false`. Do not use the demonstration password outside local development.

## API routes

All routes except `/register` and `/login` require a Bearer token.

- `GET`, `POST`, `DELETE /api/projects` (with `GET /api/projects/{id}`)
- `GET`, `POST`, `DELETE /api/projects/{projectId}/team`
- `GET`, `POST`, `DELETE /api/tasks` (with `GET /api/tasks/{id}`)
- `GET`, `POST`, `DELETE /api/work-logs` (with `GET /api/work-logs/{id}`)
- `GET`, `POST`, `DELETE /api/task-attachments`
- `GET /api/users`

Swagger is available at `http://localhost:5179/swagger` after the API starts. Import `Documentation/ProjectManagement.postman_collection.json` into Postman to test the login and project list without a REPL.

## Verification

```powershell
dotnet build KooliProjekt.sln
dotnet test KooliProjekt.Application.UnitTests
dotnet test KooliProjekt.IntegrationTests
dotnet test KooliProjekt.WindowsForms.UnitTests
dotnet test KooliProjekt.WpfApplication.UnitTests
cd KooliProjektAngular
npm run build
```

## Documentation

- `Documentation/ProjectManagement.drawio` is editable in draw.io.
- `Documentation/ProjectManagement.svg` is the diagram image.
- `Documentation/ProjectManagementDataModel.docx` documents all tables, fields and constraints.
- `coverlet.runsettings` excludes infrastructure behaviors, migrations and seed data from coverage; run `KooliProjekt.Application.UnitTests/run-tests.ps1` to produce a Cobertura report.
