using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Requests
{
    public class VideoRenameRequest
    {
        [MinLength(5)]
        public string NewName { get; set; } = null!;
    }
}