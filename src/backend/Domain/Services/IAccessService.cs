using Domain.Entities;

namespace Domain.Services;

public interface IAccessService
{
    Task<bool> CanUserUploadToEventAsync(string userId, Guid eventId);
    Task<bool> CanUserUploadToGroupAsync(string userId, Guid groupId);
    Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroupsAsync(string userId);

}