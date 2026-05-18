using System;

namespace TB.DanceDance.API.Contracts.Features.Sharing
{
    public class SharedLinkResponse
    {
        public string LinkId { get; set; }
        public Guid VideoId { get; set; }
        public string VideoName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpireAt { get; set; }
        public bool IsRevoked { get; set; }
        public string ShareUrl { get; set; }
        public bool AllowComments { get; set; }
        public bool AllowAnonymousComments { get; set; }
    }
}
