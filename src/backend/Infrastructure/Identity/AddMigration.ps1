param (
    [string]$Context = $(throw "-Context is required. Possible values: ConfigurationDbContext, PersistedGrantDbContext, IdentityStoreContext."), 
    [string]$MigrationName = $(throw "-MigrationName is required.")
)

$configContext = "ConfigurationDbContext"
$authStoreContext = "AuthStoreContext"

if ($Context -eq $identity)
{
    dotnet ef migrations add $MigrationName -c IdentityStoreContext -o Migrations/Identity
} elseif ($Context -eq $authStoreContext)
{
    dotnet ef migrations add $MigrationName -c ConfigurationDbContext -o Migrations/Openiddict
} else {
    throw new "Wrong context"
}

