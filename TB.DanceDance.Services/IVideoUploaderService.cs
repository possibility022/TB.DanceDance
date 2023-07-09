﻿using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Services;

public interface IVideoUploaderService
{
    SharedBlob GetSasUri();

    Task<VideoToTranform?> GetNextVideoToTransformAsync();
    Task<bool> UpdateVideoToTransformInformationAsync(Guid videoId, TimeSpan duration, DateTime recorded, byte[]? metadata);
    Task<Guid?> UploadConvertedVideoAsync(Guid videoToConvertId, Stream data);
    Uri GetVideoSas(string blobId);
}
