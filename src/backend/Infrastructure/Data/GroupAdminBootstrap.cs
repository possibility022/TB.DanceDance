namespace Infrastructure.Data;

/// <summary>
/// One-time data step that seeds the <c>access."GroupsAdmins"</c> table from existing
/// memberships, so groups created before admin management existed end up with admins.
/// Shipped by the <c>BootstrapGroupAdmins</c> migration; the constant is shared so tests
/// can exercise the identical SQL. Idempotent via the <c>NOT EXISTS</c> guard.
/// </summary>
public static class GroupAdminBootstrap
{
    public const string Sql =
        """
        INSERT INTO access."GroupsAdmins" ("Id", "UserId", "GroupId")
        SELECT gen_random_uuid(), a."UserId", a."GroupId"
        FROM access."AssingedToGroups" a
        WHERE NOT EXISTS (
            SELECT 1 FROM access."GroupsAdmins" ga
            WHERE ga."GroupId" = a."GroupId" AND ga."UserId" = a."UserId")
        GROUP BY a."UserId", a."GroupId";
        """;
}
