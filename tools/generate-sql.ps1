$targets = @(
    @{ Service = 'Catalog'; Context = 'CatalogDbContext'; File = 'catalog.sql' }
    @{ Service = 'Identity'; Context = 'IdentityDbContext'; File = 'identity.sql' }
    @{ Service = 'Identity'; Context = 'ConfigurationDbContext'; File = 'identity-configuration.sql' }
    @{ Service = 'Identity'; Context = 'PersistedGrantDbContext'; File = 'identity-operational.sql' }
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$outputDir = Join-Path $root 'deploy/sql'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

foreach ($target in $targets)
{
    $output = Join-Path $outputDir $target.File

    & (Join-Path $PSScriptRoot 'ef.ps1') $target.Service migrations script --idempotent --context $target.Context --output $output

    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to generate $( $target.File )"
    }
}
