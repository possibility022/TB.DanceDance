using Testcontainers.Azurite;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public static class DockerHelper
{
    private const string AzuriteImage = "mcr.microsoft.com/azure-storage/azurite:latest";
    
    private static readonly AzuriteContainer AzuriteContainer = new AzuriteBuilder()
        .WithImage(AzuriteImage)
        .Build();
    
    public static async Task<AzuriteContainer> GetInitializedAzuriteContainer()
    {
        await AzuriteContainer.StartAsync();
        return AzuriteContainer;
    }
}