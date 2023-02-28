using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.Services
{
    public interface IUserService
    {
        Task<ICollection<Event>> GetAllEvents(string user);
        Task<ICollection<Group>> GetAllGroups(string user);
        Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroups(string userName);

        Task<IEnumerable<string>> GetUserVideosAssociationsIds(string userName);

        Task<bool> UserIsAssociatedWith(string userName, string entityId);
    }
}
