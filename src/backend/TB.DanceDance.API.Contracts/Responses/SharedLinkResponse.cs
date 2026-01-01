using System;

namespace TB.DanceDance.API.Contracts.Responses
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
    }
}
