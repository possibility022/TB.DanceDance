using System;

namespace TB.DanceDance.API.Contracts.Features.Videos
{
    public class RenameVideoRequest
    {
        public Guid VideoId { get; set; }
        public string NewName { get; set; } = string.Empty;
    }
}