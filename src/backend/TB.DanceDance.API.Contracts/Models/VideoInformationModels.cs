using System;

namespace TB.DanceDance.API.Contracts.Models
{
    public class VideoFromGroupInformation : VideoInformation
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
    }
}