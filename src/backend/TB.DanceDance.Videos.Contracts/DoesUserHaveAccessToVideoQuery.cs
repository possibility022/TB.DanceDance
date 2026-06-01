using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

public record DoesUserHaveAccessToVideoQuery(string UserId, Guid VideoId) : IRequest<bool>;

public record DoesUserHaveAccessToVideoByBlobQuery(string UserId, string BlobId) : IRequest<bool>;
