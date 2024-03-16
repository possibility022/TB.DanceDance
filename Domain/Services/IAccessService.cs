using Domain.Entities;

namespace Domain.Services;
public interface IAccessService
{
    ICollection<GroupAssigmentRequest> GetGroupAssigmentRequests(string userId);
    ICollection<EventAssigmentRequest> GetEventAssigmentRequests(string userId);
}
