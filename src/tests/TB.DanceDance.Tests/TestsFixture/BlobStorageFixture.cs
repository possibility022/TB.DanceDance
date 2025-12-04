using TB.DanceDance.Tests.TestsFixture;
using Testcontainers.Azurite;

[assembly: AssemblyFixture(typeof(BlobStorageFixture))]

namespace TB.DanceDance.Tests.TestsFixture;

public class BlobStorageFixture : IAsyncLifetime
{
    private const string AzuriteImage = "mcr.microsoft.com/azure-storage/azurite";

    private const string AzuriteManuallyHostedConnectionString = 
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";

    private readonly AzuriteContainer? container;

    public BlobStorageFixture()
    {
        if (!IsStartedManually())
            container = new AzuriteBuilder()
                .WithImage(AzuriteImage)
                .Build();
    }
    
    private bool IsStartedManually() => 
        "true".Equals(Environment.GetEnvironmentVariable("ManualAzuriteConfigured"),
            StringComparison.CurrentCultureIgnoreCase);


    public string GetConnectionString()
    {
        if (container is not null)
            return container.GetConnectionString();

        return AzuriteManuallyHostedConnectionString;
    }

    public ValueTask DisposeAsync()
    {
        if (container is not null)
            return container.DisposeAsync();

        return ValueTask.CompletedTask;
    }

    public async ValueTask InitializeAsync()
    {
        if (container is not null)
            await container.StartAsync();
    }
}