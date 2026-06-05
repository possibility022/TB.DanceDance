using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Features.Sharing
{
    public class ListMySharedLinksResponse
    {
        public IReadOnlyCollection<SharedLinkResponse> Links { get; set; } = Array.Empty<SharedLinkResponse>();
    }
}