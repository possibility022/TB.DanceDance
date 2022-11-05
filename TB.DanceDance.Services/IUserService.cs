using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Services
{
    public interface IUserService
    {
        Task<UserModel> FindUserByNameAsync(string name);
        bool ValidateCredentials(string username, string password);
    }
}
