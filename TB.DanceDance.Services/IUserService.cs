using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Services
{
    public interface IUserService
    {
        Task<UserModel?> FindUserByNameAsync(string name);
        bool ValidateCredentials(string username, string password);

        Task AddUpsertUserAsync(UserModel model);

        Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroups(string userName);

        Task<IEnumerable<string>> GetUserVideosAssociationsIds(string userName);

        Task<bool> UserIsAssociatedWith(string userName, string entityId);
    }
}
