param(
    [Parameter(Mandatory, Position = 0)]
    [string]$Service,

    [Parameter(Mandatory)]
    [string]$Server,

    [Parameter(Mandatory)]
    [string]$Database,

    [Parameter(Mandatory)]
    [string]$User,

    [switch]$TrustServerCertificate,

    [switch]$CreateDatabase
)

$ErrorActionPreference = 'Stop'

$scripts = @{
    Catalog = @('catalog.sql')
    Identity = @('identity.sql', 'identity-configuration.sql', 'identity-operational.sql')
}

if (-not $scripts.ContainsKey($Service))
{
    throw "Unknown service '$Service'. Known services: $( $scripts.Keys -join ', ' )"
}

if (-not $env:SQLCMDPASSWORD)
{
    throw 'Set the password first: $env:SQLCMDPASSWORD = ''...'''
}

$root = Split-Path -Parent $PSScriptRoot
$sqlDir = Join-Path $root 'deploy/sql'

$connectionArgs = @('-S', $Server, '-U', $User, '-b')
if ($TrustServerCertificate)
{
    $connectionArgs += '-C'
}

if ($CreateDatabase)
{
    Write-Host "Creating database $Database if it does not exist ..."
    sqlcmd @connectionArgs -Q "IF DB_ID(N'$Database') IS NULL CREATE DATABASE [$Database];"

    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to create database $Database (exit code $LASTEXITCODE)"
    }
}

foreach ($file in $scripts[$Service])
{
    $path = Join-Path $sqlDir $file

    Write-Host "Applying $file to $Database ..."
    sqlcmd @connectionArgs -d $Database -i $path

    if ($LASTEXITCODE -ne 0)
    {
        throw "sqlcmd failed on $file (exit code $LASTEXITCODE)"
    }
}

Write-Host 'Done.'
