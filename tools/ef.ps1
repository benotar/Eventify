param(
    [Parameter(Mandatory, Position = 0)]
    [string]$Service,

    [Parameter(Position = 1, ValueFromRemainingArguments)]
    [string[]]$EfArgs
)

$ErrorActionPreference = 'Stop'

$services = @{
    Catalog = @{
        Project = 'src/Services/Catalog/Eventify.Catalog.Infrastructure'
        Startup = 'src/Services/Catalog/Eventify.Catalog.Api'
    }
    Identity = @{
        Project = 'src/Services/Identity/Eventify.Identity.Infrastructure'
        Startup = 'src/Services/Identity/Eventify.Identity.Web'
    }
}

if (-not $services.ContainsKey($Service))
{
    throw "Unknown service '$Service'. Known services: $( $services.Keys -join ', ' )"
}

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root $services[$Service].Project
$startup = Join-Path $root $services[$Service].Startup

Write-Host "dotnet ef $($EfArgs -join ' ') --project $project --startup-project $startup" -ForegroundColor DarkGray

dotnet ef @EfArgs --project $project --startup-project $startup
