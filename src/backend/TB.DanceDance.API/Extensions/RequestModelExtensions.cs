using Ganss.Xss;
using TB.DanceDance.API.Contracts.Requests;

namespace TB.DanceDance.API.Extensions;

public static class RequestModelExtensions
{
    extension(CreateCommentRequest request)
    {
        public CreateCommentRequest Sanitize()
        {
            HtmlSanitizer sanitizer = new HtmlSanitizer();
        
            request.Content = sanitizer.Sanitize(request.Content);
            return request;
        }
    }

    extension(UpdateCommentRequest request)
    {
        public UpdateCommentRequest Sanitize()
        {
            HtmlSanitizer sanitizer = new HtmlSanitizer();
            request.Content = sanitizer.Sanitize(request.Content);
            return request;
        }
    }
    
}