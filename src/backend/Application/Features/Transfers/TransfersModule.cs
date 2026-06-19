using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Transfers;

public static class TransfersModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTransfersFeature()
        {
            services.AddScoped<ITransferService, TransferService>();
            return services;
        }
    }
}
