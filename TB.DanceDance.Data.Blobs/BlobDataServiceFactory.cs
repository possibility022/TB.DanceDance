﻿using System;
using System.Collections.Immutable;

namespace TB.DanceDance.Data.Blobs;

public class BlobDataServiceFactory : IBlobDataServiceFactory
{
    private readonly string blobStorageConnectionString;
    private object @lock = new object();

    private ImmutableDictionary<BlobContainer, BlobDataService> cache =
        ImmutableDictionary.Create<BlobContainer, BlobDataService>();

    public BlobDataServiceFactory(string blobStorageConnectionString)
    {
        this.blobStorageConnectionString = blobStorageConnectionString;
    }

    public IBlobDataService GetBlobDataService(BlobContainer container)
    {
        var weHaveIt = cache.TryGetValue(container, out var service);
        if (weHaveIt)
        {
            return service;
        }
        else
        {
            lock (@lock)
            {
                if (!cache.ContainsKey(container))
                {
                    var name = ResolveName(container);
                    var newService = new BlobDataService(blobStorageConnectionString, name);
                    cache = cache.Add(container, newService);
                    return newService;
                }
                else
                {
                    weHaveIt = cache.TryGetValue(container, out service);
                    if (!weHaveIt)
                        throw new Exception("Could not get service from cache.");

                    return service;
                }
            }
        }
    }

    private string ResolveName(BlobContainer container)
    {
        if (container == BlobContainer.Videos)
            return "videos";
        else if (container == BlobContainer.VideosToConvert)
            // ReSharper disable once StringLiteralTypo
            return "videostoconvert";
        else
            throw new ArgumentOutOfRangeException(nameof(container),
                "Could not resolve name for blob container type: " + container.ToString());
    }
}

public enum BlobContainer
{
    Videos,
    VideosToConvert
}

public interface IBlobDataServiceFactory
{
    IBlobDataService GetBlobDataService(BlobContainer container);
}