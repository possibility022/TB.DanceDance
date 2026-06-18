using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Competitions;

public static class CompetitionsModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCompetitionsFeature()
        {
            services.AddScoped<ICompetitionService, CompetitionService>();
            return services;
        }
    }
}
