using System;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Features.Groups.Model
{
    public class VideoFromGroupInformation : VideoInformation
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
    }
}