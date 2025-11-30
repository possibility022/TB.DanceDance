using Serilog;

namespace TB.DanceDance.Mobile.Library.Services.Network;

public class BackupServerHttpHandler : DelegatingHandler
{
    private readonly ServersConfiguration configuration;
    private bool useBackupServer;
    private DateTime? nextPrimaryCheck;
    private Task? hostCheck;

    public BackupServerHttpHandler(ServersConfiguration configuration, HttpMessageHandler innerHandler) :
        base(innerHandler)
    {
        this.configuration = configuration;
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CheckAndRebuildRequestUri(request);

        try
        {
            var responseMessage = base.Send(request, cancellationToken);
            if ((int)responseMessage.StatusCode < 500)
                return responseMessage;

            StartTaskToCheckServers();
        
            return responseMessage;
        }
        catch
        {
            StartTaskToCheckServers();
            throw;
        }
        
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CheckAndRebuildRequestUri(request);

        try
        {
            var responseMessage = await base.SendAsync(request, cancellationToken);
            if ((int)responseMessage.StatusCode < 500)
                return responseMessage;
            
            StartTaskToCheckServers();
            return responseMessage;
        }
        catch
        {
            StartTaskToCheckServers();
            throw;
        }

    }

    private void CheckAndRebuildRequestUri(HttpRequestMessage request)
    {
        if (useBackupServer)
        {
            if (nextPrimaryCheck is not null && nextPrimaryCheck < DateTime.Now)
                StartTaskToCheckServers();
            
            if (request.RequestUri is not null)
                request.RequestUri = RebuildToUseSecondary(request.RequestUri);
        }
    }

    private Uri RebuildToUseSecondary(Uri requestUri)
    {
        var builder = new UriBuilder(requestUri)
        {
            Host = configuration.Secondary.Host,
            Port = configuration.Secondary.Port,
            Scheme = configuration.Secondary.Scheme,
            Query = requestUri.Query,
            Fragment = requestUri.Fragment,
            Path =  requestUri.AbsolutePath
        };
        return builder.Uri;
    }

    private void StartTaskToCheckServers()
    {
        if (hostCheck?.IsCompleted is false)
        {
            Log.Information("Another check server is running.");
            return;
        }

        hostCheck = Task.Run(async () =>
        {
            var primaryUri = BuildUri(configuration.Primary, configuration.HealthEndpoint);
            var primaryIsUp = await CheckIfServerIsUp(primaryUri);

            nextPrimaryCheck = DateTime.Now.AddMinutes(45);

            if (primaryIsUp)
            {
                useBackupServer = false;
                return;
            }

            var secondaryUri = BuildUri(configuration.Secondary, configuration.HealthEndpoint);
            var secondaryIsUp = await CheckIfServerIsUp(secondaryUri);
            if (secondaryIsUp)
                useBackupServer = true;
        });
    }

    private static Uri BuildUri(Uri host, string path)
    {
        var uriBuilder = new UriBuilder(host) { Path = path };
        return uriBuilder.Uri;
    }

    private async Task<bool> CheckIfServerIsUp(Uri uri)
    {
        try
        {
            using var httpClient = new HttpClient();


            var response = await httpClient.GetAsync(uri);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error when checking if primary server is up");
            return false;
        }
    }
}