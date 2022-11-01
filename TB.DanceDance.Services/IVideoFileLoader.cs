using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.Services
{
    public interface IVideoFileLoader
    {
        Task<VideoInformation> CreateRecord(string filePath);
    }

    public class FakeFileLoader : IVideoFileLoader
    {
        public Task<VideoInformation> CreateRecord(string filePath)
        {
            throw new NotSupportedException();
        }
    }
}