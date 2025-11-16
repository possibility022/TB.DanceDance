using Domain.Entities;

namespace Domain.Services;

public interface IAccessService
{
    Task<bool> CanUserUploadToEventAsync(string userId, Guid eventId, CancellationToken cancellationToken);
    Task<bool> CanUserUploadToGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken);
    Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroupsAsync(string userId, CancellationToken cancellationToken);
    Task<bool> DoesUserHasAccessToEvent(Guid eventId, string userId, CancellationToken cancellationToken);
    Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId, CancellationToken cancellationToken);
    Task<bool> DoesUserHasAccessAsync(Guid videoId, string userId, CancellationToken cancellationToken);
}