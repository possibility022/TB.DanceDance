using System;

namespace Application.Features.Videos.Endpoints.Videos
{
    public class RenameVideoRequest
    {
        public Guid VideoId { get; set; }
        public string NewName { get; set; } = string.Empty;
    }
}