param (
    [string]$Context = $(throw "-Context is required. Possible values: ConfigurationDbContext, PersistedGrantDbContext, IdentityStoreContext."), 
    [string]$MigrationName = $(throw "-MigrationName is required.")
)

$configContext = "ConfigurationDbContext"
$pgContext = "PersistedGrantDbContext"
$identity = "IdentityStoreContext"

if ($Context -eq $identity)
{
    dotnet ef migrations add $MigrationName -c IdentityStoreContext -o Migrations/Identity/IdentityStore
} elseif ($Context -eq $pgContext)
{
    dotnet ef migrations add $MigrationName -c ConfigurationDbContext -o Migrations/IdentityServer/ConfigurationDb
} elseif ($Context -eq $configContext)
{
    dotnet ef migrations add $MigrationName -c PersistedGrantDbContext -o Migrations/IdentityServer/PersistedGrantDb
} else {
    throw new "Wrong context"
}

