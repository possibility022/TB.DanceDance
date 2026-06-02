using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Comments;

public static class CommentsModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCommentsFeature()
        {
            services.AddScoped<ICommentService, CommentService>();
            return services;
        }
    }
}
