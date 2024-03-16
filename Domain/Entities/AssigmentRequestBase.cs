﻿namespace Domain.Entities;
public abstract class AssigmentRequestBase
{
    public required string UserId { get; init; }

    /// <summary>
    /// When <code>true</code>, then approved.
    /// When <code>false</code> then rejected.
    /// When <code>null</code> then no action taken.
    /// </summary>
    public bool? Approved { get; set; } = false;

    /// <summary>
    /// A user who accepted or rejected request.
    /// </summary>
    public string? ManagedBy { get; set; } = null;
}