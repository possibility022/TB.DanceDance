using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Services
{
    public interface IVideoFileLoader
    {
        Task<(Video, string)> CreateRecord(string filePath);
    }

    public class FakeFileLoader : IVideoFileLoader
    {
        public Task<(Video, string)> CreateRecord(string filePath)
        {
            throw new NotSupportedException();
        }
    }
}