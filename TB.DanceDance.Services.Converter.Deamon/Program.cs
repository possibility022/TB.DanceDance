﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TB.DanceDance.Services.Converter.Deamon;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;

ProgramConfig.Configure();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(ProgramConfig.Instance);
builder.Services.AddSingleton<OAuthHttpClient>((s) =>
{
    return new OAuthHttpClient()
    {
        BaseAddress = new Uri(ProgramConfig.Instance.OAuthOrigin)
    };
});
builder.Services.AddSingleton<ApiHttpClient>((s) =>
{
    var tokenProvider = new TokenProvider(s.GetRequiredService<OAuthHttpClient>(), ProgramConfig.Instance.TokenProviderOptions);
    var handler = new TokenHttpHandler(tokenProvider);

    var apiHttpClient = new ApiHttpClient(handler, true)
    {
        Timeout = TimeSpan.FromSeconds(60 * 5),
        BaseAddress = new Uri(ProgramConfig.Instance.ApiOrigin)
    };

    return apiHttpClient;
});

builder.Services.AddScoped<HttpClient>();
builder.Services.AddScoped<DanceDanceApiClient>();
builder.Services.AddScoped<Deamon>();

builder.Services.AddHostedService<Deamon>();

IHost host = builder.Build();
host.Run();


