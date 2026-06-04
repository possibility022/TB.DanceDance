using System;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.Groups.Models
{
    public class VideoFromGroupInformation : VideoInformation
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
    }
}