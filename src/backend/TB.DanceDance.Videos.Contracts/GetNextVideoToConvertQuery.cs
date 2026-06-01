using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

public record GetNextVideoToConvertQuery : IRequest<VideoToConvertDto?>;
