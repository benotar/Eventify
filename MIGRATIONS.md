# Database Migrations

Each service owns **its own database** and **its own migrations**, which live inside that service's Infrastructure project.

The workflow in one line: **generate migrations with EF, turn them into SQL scripts, commit the scripts, and apply the scripts yourself.**
We do not use `dotnet ef database update` — every database (local or Azure) gets exactly the SQL that was reviewed in the pull request.

All commands run **from the repository root**.

---

## Prerequisites

- .NET SDK (version pinned in `global.json`)
- the `dotnet-ef` tool, pinned in `dotnet-tools.json` at the repository root:
  ```powershell
  dotnet tool restore
  ```
- Docker Desktop — for local databases
- PowerShell (the scripts are `.ps1`; they will not run in Git Bash)
- `sqlcmd` — used by `tools/apply-sql.ps1` to run the SQL scripts:
  ```powershell
  winget install sqlcmd
  ```
  Open a **new** terminal after installing (restart Rider if you use its terminal) — already running processes do not see the updated `PATH`.

If PowerShell refuses to run `.ps1` files with an `execution policy` error:

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

---

## Scripts

| Script                   | What it does                                                                                  |
|--------------------------|-----------------------------------------------------------------------------------------------|
| `tools/ef.ps1`           | Runs `dotnet ef` for a service, filling in `--project` and `--startup-project`                |
| `tools/generate-sql.ps1` | Regenerates the idempotent SQL script for every context into `deploy/sql/`                    |
| `tools/apply-sql.ps1`    | Runs a service's scripts from `deploy/sql/` against a database, optionally creating it first  |

### `tools/ef.ps1`

```
.\tools\ef.ps1 <Service> <arguments for dotnet ef>
```

The first word is the service name (`Catalog`, `Identity`). Everything after it is passed to `dotnet ef` unchanged.

```powershell
# without the script
dotnet ef migrations add AddVenue -p src/Services/Catalog/Eventify.Catalog.Infrastructure -s src/Services/Catalog/Eventify.Catalog.Api

# with the script
.\tools\ef.ps1 Catalog migrations add AddVenue --context CatalogDbContext --output-dir Persistence/Migrations
```

> **Use long option names only:** `--output-dir`, `--context`.
> PowerShell intercepts the short `-o` as a parameter of the script itself (it matches the built-in
> `-OutVariable` / `-OutBuffer` parameters) and fails with `parameter name 'o' is ambiguous`.

---

## Contexts

| Service  | `--context`               | `--output-dir`                                  | SQL script                              |
|----------|---------------------------|-------------------------------------------------|-----------------------------------------|
| Catalog  | `CatalogDbContext`        | `Persistence/Migrations`                        | `deploy/sql/catalog.sql`                |
| Identity | `IdentityDbContext`       | `Persistence/Migrations/Identity`               | `deploy/sql/identity.sql`               |
| Identity | `ConfigurationDbContext`  | `Persistence/Migrations/Identity/Configuration` | `deploy/sql/identity-configuration.sql` |
| Identity | `PersistedGrantDbContext` | `Persistence/Migrations/Identity/Operational`   | `deploy/sql/identity-operational.sql`   |

`--output-dir` is relative to the service's Infrastructure project.
`ConfigurationDbContext` and `PersistedGrantDbContext` belong to Duende IdentityServer; their migrations live in our assembly because of `MigrationsAssembly(...)` in the Identity service's `DependencyInjection.cs`.
All three Identity scripts go into the **same** Identity database.

---

## Changing the schema — step by step

1. Change the entity or its EF configuration.
2. [Add a migration](#add-a-migration).
3. Review the generated `*.cs`.
4. [Regenerate the SQL scripts](#regenerate-the-sql-scripts) and review the diff in `deploy/sql/`.
5. **Commit the migration and the changed `deploy/sql/*.sql` in the same commit.**
6. [Apply the scripts](#apply-migrations) to your local database.

---

## Add a migration

```powershell
.\tools\ef.ps1 Catalog migrations add <MigrationName> --context CatalogDbContext --output-dir Persistence/Migrations
```

```powershell
.\tools\ef.ps1 Identity migrations add <MigrationName> --context IdentityDbContext --output-dir Persistence/Migrations/Identity
.\tools\ef.ps1 Identity migrations add <MigrationName> --context ConfigurationDbContext --output-dir Persistence/Migrations/Identity/Configuration
.\tools\ef.ps1 Identity migrations add <MigrationName> --context PersistedGrantDbContext --output-dir Persistence/Migrations/Identity/Operational
```

**Naming:** PascalCase describing the change — `AddVenue`, `AddVenueNameIndex`, `RenameArtistBio`.
EF prepends the timestamp to the file name itself.

**Always pass `--context` and `--output-dir`** so the new migration lands next to the existing ones.

### Review the generated migration

Open the generated `*.cs` and read `Up()`:

- `DropColumn` + `AddColumn` where you actually **renamed** a property → EF does not detect renames and the column's data will be lost. Replace it manually with `migrationBuilder.RenameColumn(...)`.
- a new `NOT NULL` column on a table that already has rows, with no default value → fails on a populated table.
- `DropColumn` / `DropTable` — make sure it is intentional.

### Indexes on complex-type properties

EF Core 10 cannot index properties of a complex type (for example `Venue.Address.City`) with `HasIndex`.
Catalog uses the `EFCore.ComplexIndexes.SqlServer` package for that: declare the index with `HasComplexIndex()` in the entity configuration, and `migrations add` generates the `CreateIndex` itself. Nothing has to be written by hand in the migration.

---

## Regenerate the SQL scripts

```powershell
.\tools\generate-sql.ps1
```

Regenerates all files in `deploy/sql/` from the current migrations. The scripts are **idempotent**: before each migration they check the `__EFMigrationsHistory` table and apply only what the database does not have yet, so running a script again is safe.

- **Never edit `deploy/sql/*.sql` by hand** — the next regeneration overwrites them. Change the migration instead and regenerate.
- The files are stored in git with LF line endings (see `.gitattributes`). A `CRLF will be replaced by LF` warning on `git add` is expected.
- The files are committed so that anyone can set up a database from them without .NET or the EF tools.

---

## Remove a migration

```powershell
.\tools\ef.ps1 Catalog migrations remove --context CatalogDbContext
.\tools\generate-sql.ps1
```

Removes the **latest** migration and reverts the model snapshot (`*ModelSnapshot.cs`). Regenerate the SQL scripts afterwards.

> Only if the migration has **not been applied to any database yet**. If it has, do not remove it — add a new migration that reverts the change.

---

## Apply migrations

**Take the scripts from `deploy/sql/` and run them yourself** against the target database with `tools/apply-sql.ps1`. The same files are used locally and on Azure.

### `tools/apply-sql.ps1`

```
.\tools\apply-sql.ps1 <Service> -Server <server> -Database <database> -User <user> [-TrustServerCertificate] [-CreateDatabase]
```

| Parameter                 | Meaning                                                                                                       |
|---------------------------|---------------------------------------------------------------------------------------------------------------|
| `<Service>`               | `Catalog` or `Identity` — decides which files from `deploy/sql/` are run                                      |
| `-Server`                 | SQL Server address. `'localhost,1433'`                                                                        |
| `-Database`               | The service's database. The scripts run **inside it**, never in `master`. `eventify-<service-name>`           |
| `-User`                   | SQL login. `sa`                                                                                               |
| `-TrustServerCertificate` | Skip server certificate validation. **Local Docker only** — its certificate is self-signed                    |
| `-CreateDatabase`         | Create the database first if it does not exist. **Local only** — on Azure databases are created in the Portal |

The password is **not** a parameter. `sqlcmd` reads it from the `SQLCMDPASSWORD` environment variable, and the script stops with an error if the variable is not set. Passing a password with `-P` is insecure: it is visible in the process list and stays in the console history.

```powershell
$env:SQLCMDPASSWORD = '<password>'
# ... run apply-sql.ps1 ...
Remove-Item Env:SQLCMDPASSWORD
```

`$env:SQLCMDPASSWORD` lives **only in the current PowerShell window**; `Remove-Item Env:...` removes it. Put the password in **single** quotes, so a `$` inside it is not treated as a variable.

For Identity the script runs all three files (`identity.sql`, `identity-configuration.sql`, `identity-operational.sql`) into the same database.
It stops at the first SQL error, so the next files are not applied on top of a failed one.

### Locally (Docker)

| Service  | Server           | Database            |
|----------|------------------|---------------------|
| Catalog  | `localhost,1433` | `eventify-catalog`  |
| Identity | `localhost,1433` | `eventify-identity` |

Both databases live in the single SQL Server container `eventify_db`.
The source of truth is `Database:ConnectionString` in the service's `appsettings.Development.json`. The SA password is in `deploy/docker/compose.yml`.

**1. Start SQL Server**

```powershell
docker compose -f deploy/docker/compose.yml up -d
docker compose -f deploy/docker/compose.yml ps
```

Wait until the status is `healthy`.

**2. Apply the scripts**

First time — create the databases and apply:

```powershell
$env:SQLCMDPASSWORD = '<SA password from deploy/docker/compose.yml>'
.\tools\apply-sql.ps1 Catalog  -Server 'localhost,1433' -Database eventify-catalog  -User sa -TrustServerCertificate -CreateDatabase
.\tools\apply-sql.ps1 Identity -Server 'localhost,1433' -Database eventify-identity -User sa -TrustServerCertificate -CreateDatabase
Remove-Item Env:SQLCMDPASSWORD
```

Afterwards — every time new migrations arrive (after `git pull` or after your own `generate-sql.ps1`), run the same commands. `-CreateDatabase` can stay: it does nothing if the database already exists.

`'localhost,1433'` **must be quoted**. Without quotes PowerShell treats the comma as an array separator and passes `localhost` and `1433` as two separate values.

**Expected warnings.** Applying `identity.sql` prints two warnings like
`The maximum key length for a clustered index is 900 bytes. The index 'PK_AspNetUserLogins' has maximum length of 1800 bytes.`
They come from the standard ASP.NET Core Identity schema (key columns are `nvarchar(450)`). SQL Server checks the limit against the actual data, and real values (`Google`, a provider user id) are far below 900 bytes. The script still succeeds.

### Azure

```powershell
$env:SQLCMDPASSWORD = '<Azure SQL password>'
.\tools\apply-sql.ps1 Catalog -Server <server>.database.windows.net -Database eventify-catalog -User <user>
Remove-Item Env:SQLCMDPASSWORD
```

- **No `-TrustServerCertificate`** — Azure has a valid certificate and it should be checked.
- **No `-CreateDatabase`** — the database must already exist, created in Azure Portal with the free offer. A database created with `CREATE DATABASE` is not on the free offer.
- Your IP address must be allowed in the server firewall (Azure Portal → the server → **Networking**).
- A paused serverless database can take up to a minute to wake up — if the first connection times out, just run the script again. It is idempotent.

> **Never commit** the Azure connection string or password to the repository.

### Without the script (Rider)

The scripts are plain SQL, so any SQL client can run them. In Rider:

1. Database tool window → add a SQL Server data source (locally: host `localhost`, port `1433`, user `sa`, the SA password, trust the server certificate).
2. If the database does not exist yet, open a console on the data source and run `CREATE DATABASE [eventify-catalog];` (locally only).
3. Open the script from `deploy/sql/`, attach it to the data source, **select the service's database** (not `master`) and run the whole file.

For Identity, run all three Identity scripts against `eventify-identity`.
