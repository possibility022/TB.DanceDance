using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

public record GetVideoForViewingQuery(string UserId, string BlobId) : IRequest<VideoDto?>;
