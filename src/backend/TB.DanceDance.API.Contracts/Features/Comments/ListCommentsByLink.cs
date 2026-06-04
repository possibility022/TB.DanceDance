using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Features.Comments;

namespace Application.Features.Comments.Endpoints
{
    public class ListCommentsByLink
    {
        /// <summary>Shared link id (bound from the route).</summary>
        public string LinkId { get; set; } = null!;

        /// <summary>Anonymous id (bound from the query string); falls back to the request header.</summary>
        public string? AnonymousId { get; set; }
    }
    
    public class ListCommentsByLinkResponse
    {
        public IReadOnlyCollection<CommentResponse> Comments { get; set; } = Array.Empty<CommentResponse>();
    }
}