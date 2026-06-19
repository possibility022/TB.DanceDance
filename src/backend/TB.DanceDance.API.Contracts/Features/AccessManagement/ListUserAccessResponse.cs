using System;
using TB.DanceDance.API.Contracts.Features.AccessManagement.Models;

namespace TB.DanceDance.API.Contracts.Features.AccessManagement
{
    public class GetUserAccessResponse
    {
        public GetUserAccessSet Assigned { get; set; } = null!;
        public GetUserAccessSet Available { get; set; } = null!;
        public ListUserAccessPending Pending { get; set; } = null!;

        /// <summary>
        /// Ids of the assigned groups the current user administers. The web app uses this to
        /// surface a "Manage" entry point without a separate "my admin groups" endpoint.
        /// </summary>
        public Guid[] AdministeredGroupIds { get; set; } = Array.Empty<Guid>();
    }
}