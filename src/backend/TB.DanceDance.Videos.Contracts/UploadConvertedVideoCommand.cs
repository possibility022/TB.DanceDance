using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

public record UploadConvertedVideoCommand(Guid VideoId) : IRequest<Guid?>;
