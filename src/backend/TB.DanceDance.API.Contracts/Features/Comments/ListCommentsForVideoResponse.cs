using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Features.Comments
{
    public class ListCommentsForVideoResponse
    {
        public IReadOnlyCollection<CommentResponse> Comments { get; set; } = Array.Empty<CommentResponse>();
    }
}