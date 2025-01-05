using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Responses
{
    public class VideoInformationResponse : VideoInformationModel
    {

    }

    public class GroupWithVideosResponse
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = null!;
        public ICollection<VideoInformationModel> Videos { get; set; } = null!;
    }

    public class VideoInformationModel
    {
        public Guid Id { get; set; }
        public string BlobId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime RecordedDateTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool Converted { get; set; }
    }
}
