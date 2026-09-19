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

$efScript = Join-Path $PSScriptRoot 'ef.ps1'

foreach ($target in $targets)
{
    & $efScript $target.Service migrations has-pending-model-changes --context $target.Context

    if ($LASTEXITCODE -ne 0)
    {
        throw "$( $target.Context ): 'has-pending-model-changes' failed — either the model has changes without a migration, or the command itself failed. See the output above."
    }

    $output = Join-Path $outputDir $target.File

    & $efScript $target.Service migrations script --idempotent --context $target.Context --output $output

    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to generate $( $target.File )"
    }
}
