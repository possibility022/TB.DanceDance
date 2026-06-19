namespace Application.Features.Groups;

/// <summary>
/// Outcome of attempting to remove a group admin. Distinguishes the last-admin guard
/// (a group must always keep at least one admin) from a plain no-op.
/// </summary>
public enum RemoveGroupAdminResult
{
    Removed,
    NotAnAdmin,
    BlockedLastAdmin,
}
