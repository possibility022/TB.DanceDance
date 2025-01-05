param (
    [string]$ConnectionString = $null
)

# I HAVE NO IDEA WHY IT DOES NOT WORK. Just copy it and paste

if ($null -eq $ConnectionString)
{
    dotnet ef database update --context IdentityStoreContext
    dotnet ef database update --context ConfigurationDbContext
    dotnet ef database update --context PersistedGrantDbContext
} else {
    dotnet ef database update --context IdentityStoreContext --connection $ConnectionString
    dotnet ef database update --context ConfigurationDbContext --connection $ConnectionString
    dotnet ef database update --context PersistedGrantDbContext --connection $ConnectionString
}
