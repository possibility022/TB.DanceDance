using TB.DanceDance.Tests.TestsFixture;
using Testcontainers.Azurite;

[assembly: AssemblyFixture(typeof(BlobStorageFixture))]

namespace TB.DanceDance.Tests.TestsFixture;

public class BlobStorageFixture() : IAsyncLifetime
{
    private const string AzuriteImage = "mcr.microsoft.com/azure-storage/azurite";
    
    private readonly AzuriteContainer container = new AzuriteBuilder()
        .WithImage(AzuriteImage)
        .Build();


    public string GetConnectionString()
    {
        return container.GetConnectionString();
    }

    public ValueTask DisposeAsync()
    {
        return container.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
    }
}