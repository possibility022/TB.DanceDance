using System.Diagnostics;
using System.Reflection;
using TB.DanceDance.Mobile.Library.Services.Network;
using WireMock.RequestBuilders;
using WireMock.Server;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class BackupServerHttpHandlerTests : IDisposable
{
    private readonly WireMockServer primary;
    private readonly WireMockServer secondary;
    private readonly BackupServerHttpHandler backupServerHttpHandler;

    private const string HealthEndpoint = "/health";

    private readonly IRequestBuilder healthRequestBuilder
        = Request.Create().WithPath(HealthEndpoint).UsingGet();

    private const string RegularEndpoint = "/regular";

    private readonly IRequestBuilder regularRequestBuilder
        = Request.Create().WithPath(RegularEndpoint).UsingGet();

    public BackupServerHttpHandlerTests()
    {
        primary = WireMockServer.Start();
        secondary = WireMockServer.Start();

        primary.Given(healthRequestBuilder).ThenRespondWithOK();
        secondary.Given(healthRequestBuilder).ThenRespondWithOK();

        primary.Given(regularRequestBuilder).ThenRespondWithOK();
        secondary.Given(regularRequestBuilder).ThenRespondWithOK();


        backupServerHttpHandler = new BackupServerHttpHandler(
            new ServersConfiguration()
            {
                HealthEndpoint = HealthEndpoint,
                Primary = new Uri(primary.Url!),
                Secondary = new Uri(secondary.Url!)
            }, new SocketsHttpHandler());
    }

    private Uri CreateRequestUri(WireMockServer server)
    {
        var uriBuilder = new UriBuilder(server.Url!);
        uriBuilder.Path = RegularEndpoint; // It should not be a health endpoint.
        return uriBuilder.Uri;
    }

    [Fact]
    public async Task Base_EnsureNormalRequestWorks_ToPrimary()
    {
        using var client = new HttpClient(backupServerHttpHandler);

        var res = await client.GetAsync(CreateRequestUri(primary), TestContext.Current.CancellationToken);
        res.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Base_EnsureNormalRequestWorks_ToSecondary()
    {
        using var client = new HttpClient(backupServerHttpHandler);

        var res = await client.GetAsync(CreateRequestUri(secondary), TestContext.Current.CancellationToken);
        res.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task WhenPrimaryIsDown_BackupServerIsCheck_ForAsyncMethod()
    {
        var requestUri = CreateRequestUri(primary);

        await WhenPrimaryIsDown_BackupServerIsCheck_Body(c =>
            c.GetAsync(requestUri, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WhenPrimaryIsDown_BackupServerIsCheck_ForSyncMethod()
    {
        var requestUri = CreateRequestUri(primary);

        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        await WhenPrimaryIsDown_BackupServerIsCheck_Body(c => Task.FromResult(c.Send(request)));
    }

    private async Task WhenPrimaryIsDown_BackupServerIsCheck_Body(Func<HttpClient, Task> func)
    {
        using var client = new HttpClient(backupServerHttpHandler);

        primary.Stop();

        try
        {
            await func(client);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        var task = GetCheckHostTask(backupServerHttpHandler);
        Assert.NotNull(task);

        await task;

        Assert.Single(secondary.LogEntries);
        Assert.Equal(HealthEndpoint, secondary.LogEntries[0].RequestMessage.Path);

        var switchToSecondary = GetOrSetUseBackupServerFlag(backupServerHttpHandler);
        Assert.True(switchToSecondary);
    }
    
    [Fact]
    public async Task WhenPrimaryReturnsInternalError_BackupServerIsCheck_ForAsyncMethod()
    {
        var requestUri = CreateRequestUri(primary);

        await WhenPrimaryReturnsInternalError_BackupServerIsCheck_Body(c =>
            c.GetAsync(requestUri, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WhenPrimaryReturnsInternalError_BackupServerIsCheck_ForSyncMethod()
    {
        var requestUri = CreateRequestUri(primary);

        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        await WhenPrimaryReturnsInternalError_BackupServerIsCheck_Body(c => Task.FromResult(c.Send(request)));
    }

    private async Task WhenPrimaryReturnsInternalError_BackupServerIsCheck_Body(Func<HttpClient, Task> func)
    {
        using var client = new HttpClient(backupServerHttpHandler);

        primary.Reset();
        primary.Given(regularRequestBuilder).ThenRespondWithStatusCode(500);

        await func(client);

        var task = GetCheckHostTask(backupServerHttpHandler);
        Assert.NotNull(task);

        await task;

        Assert.Single(secondary.LogEntries);
        Assert.Equal(HealthEndpoint, secondary.LogEntries[0].RequestMessage.Path);

        var switchToSecondary = GetOrSetUseBackupServerFlag(backupServerHttpHandler);
        Assert.True(switchToSecondary);
    }

    [Fact]
    public async Task WhenUsingBackup_RewriteUrl_ForAsyncMethod()
    {
        var requestUri = CreateRequestUri(primary);

        await WhenUsingBackup_RewriteUrl_Body(c => c.GetAsync(requestUri, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WhenUsingBackup_RewriteUrl_ForSyncMethod()
    {
        var requestUri = CreateRequestUri(primary);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        await WhenUsingBackup_RewriteUrl_Body(c => Task.FromResult(c.Send(request)));
    }

    [Fact]
    public async Task WhenUsingBackup_CheckPrimaryAfter45Min_ForAsyncMethod()
    {
        var requestUri = CreateRequestUri(primary);

        await WhenUsingBackup_CheckPrimaryAfter45Min_Body((c) =>
            c.GetAsync(requestUri, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WhenUsingBackup_CheckPrimaryAfter45Min_ForSyncMethod()
    {
        var requestUri = CreateRequestUri(primary);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        await WhenUsingBackup_CheckPrimaryAfter45Min_Body(c => Task.FromResult(c.Send(request)));
    }

    private async Task WhenUsingBackup_CheckPrimaryAfter45Min_Body(Func<HttpClient, Task> func)
    {
        using var client = new HttpClient(backupServerHttpHandler);

        GetOrSetUseBackupServerFlag(backupServerHttpHandler, true);
        GetAndSetLastPrimaryCheckDate(backupServerHttpHandler, DateTime.UtcNow.AddMinutes(-46));

        await func(client);

        var hostCheckTask = GetCheckHostTask(backupServerHttpHandler);

        Assert.NotNull(hostCheckTask);
        await hostCheckTask;

        var useBackupServerFlag = GetOrSetUseBackupServerFlag(backupServerHttpHandler);
        Assert.False(useBackupServerFlag);
        Assert.Single(primary.LogEntries);
        Assert.Equal(HealthEndpoint, primary.LogEntries[0].RequestMessage.Path);
    }
    
    [Fact]
    public async Task WhenSwitchedToBackup_NextCheckForPrimaryDateIsSet_ForSyncMethod()
    {
        var requestUri = CreateRequestUri(primary);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        
        await WhenSwitchedToBackup_NextCheckForPrimaryDateIsSet_Body(c => Task.FromResult(c.Send(request)));
    }
    
    [Fact]
    public async Task WhenSwitchedToBackup_NextCheckForPrimaryDateIsSet_ForAyncMethod()
    {
        var requestUri = CreateRequestUri(primary);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        
        await WhenSwitchedToBackup_NextCheckForPrimaryDateIsSet_Body(c => c.SendAsync(request));
    }
    
    private async Task WhenSwitchedToBackup_NextCheckForPrimaryDateIsSet_Body(Func<HttpClient, Task> func)
    {
        using var client = new HttpClient(backupServerHttpHandler);

        primary.Reset();
        primary.Given(regularRequestBuilder).ThenRespondWithStatusCode(500);

        await func(client);

        var task = GetCheckHostTask(backupServerHttpHandler);
        Assert.NotNull(task);

        await task;

        var nextPrimaryCheckDate = GetAndSetLastPrimaryCheckDate(backupServerHttpHandler, DateTime.Now);
        
        Assert.NotNull(nextPrimaryCheckDate);
        Assert.True(nextPrimaryCheckDate > DateTime.Now.AddMinutes(40), "nextPrimaryCheckDate > DateTime.Now.AddMinutes(40)");
        Assert.True(nextPrimaryCheckDate < DateTime.Now.AddMinutes(46), "nextPrimaryCheckDate < DateTime.Now.AddMinutes(46)");
    }

    private async Task WhenUsingBackup_RewriteUrl_Body(Func<HttpClient, Task> func)
    {
        using var client = new HttpClient(backupServerHttpHandler);

        GetOrSetUseBackupServerFlag(backupServerHttpHandler, true);

        await func(client);

        Assert.Empty(primary.LogEntries);
        Assert.Single(secondary.LogEntries);
        Assert.Equal(RegularEndpoint, secondary.LogEntries[0].RequestMessage.Path);
    }

    private Task? GetCheckHostTask(BackupServerHttpHandler handler)
    {
        Type type = handler.GetType();

        // Get the private field "hostCheck"
        FieldInfo? fieldInfo = type.GetField("hostCheck", BindingFlags.NonPublic | BindingFlags.Instance);

        if (fieldInfo is null)
            throw new InvalidOperationException("Could not get private field");

        // Read the value
        var value = (Task?)fieldInfo.GetValue(handler);
        return value;
    }

    private DateTime? GetAndSetLastPrimaryCheckDate(BackupServerHttpHandler handler, DateTime lastPrimaryCheckDate)
    {
        Type type = handler.GetType();

        // Get the private field "hostCheck"
        FieldInfo? fieldInfo = type.GetField("nextPrimaryCheck", BindingFlags.NonPublic | BindingFlags.Instance);

        if (fieldInfo is null)
            throw new InvalidOperationException("Could not get private field");

        // Read the value
        var value = (DateTime?)(fieldInfo.GetValue(handler));

        fieldInfo.SetValue(handler, lastPrimaryCheckDate);

        return value;
    }

    private bool GetOrSetUseBackupServerFlag(BackupServerHttpHandler handler, bool? valueToSet = null)
    {
        Type type = handler.GetType();

        // Get the private field "hostCheck"
        FieldInfo? fieldInfo = type.GetField("useBackupServer", BindingFlags.NonPublic | BindingFlags.Instance);

        if (fieldInfo is null)
            throw new InvalidOperationException("Could not get private field");

        // Read the value
        var value = (bool)(fieldInfo.GetValue(handler) ??
                           throw new InvalidOperationException("Could not get private field"));

        if (valueToSet.HasValue)
            fieldInfo.SetValue(handler, valueToSet);

        return value;
    }

    public void Dispose()
    {
        primary.Dispose();
        secondary.Dispose();
        backupServerHttpHandler.Dispose();
    }
}