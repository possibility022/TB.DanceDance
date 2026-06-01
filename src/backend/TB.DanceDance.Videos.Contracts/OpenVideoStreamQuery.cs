using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

public record OpenVideoStreamQuery(string BlobName) : IRequest<Stream>;
