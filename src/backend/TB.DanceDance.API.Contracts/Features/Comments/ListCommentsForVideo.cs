using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Features.Comments
{
    public class ListCommentsForVideoRequest
    {
        /// <summary>The video id (bound from the route).</summary>
        public Guid VideoId { get; set; }
    }
    
    public class ListCommentsForVideoResponse
    {
        public IReadOnlyCollection<CommentResponse> Comments { get; set; } = Array.Empty<CommentResponse>();
    }
}