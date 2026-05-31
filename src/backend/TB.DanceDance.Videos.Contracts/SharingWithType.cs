namespace TB.DanceDance.Videos.Contracts;

/// <summary>
/// Determines who a newly uploaded video is shared with. Maps to (EventId, GroupId)
/// in the Videos module: Group sets GroupId, Event sets EventId, Private leaves both null.
/// </summary>
public enum SharingWithType
{
    NotSpecified,
    Group,
    Event,
    Private
}
