$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
dotnet test "$projectRoot\KooliProjekt.Application.UnitTests\KooliProjekt.Application.UnitTests.csproj" --settings "$projectRoot\coverlet.runsettings" --collect:"XPlat Code Coverage" --results-directory "$projectRoot\TestResults"
