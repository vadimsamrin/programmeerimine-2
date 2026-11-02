# 17 October 2025

`ApplicationDbContext` contains DbSets for projects, team members, tasks, work logs and task attachments. The initial EF Core migration is in `../../KooliProjekt.Application/Migrations`.

To apply it after SQL Server LocalDB is installed:

```powershell
dotnet tool run dotnet-ef database update --project KooliProjekt.Application --startup-project KooliProjekt.WebApi
```
