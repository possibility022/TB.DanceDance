using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Features.Sharing;

namespace Application.Features.Sharing.Endpoints
{
    public class ListMySharedLinksResponse
    {
        public IReadOnlyCollection<SharedLinkResponse> Links { get; set; } = Array.Empty<SharedLinkResponse>();
    }
}