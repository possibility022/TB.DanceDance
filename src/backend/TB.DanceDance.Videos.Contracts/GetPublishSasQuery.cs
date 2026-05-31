using TB.DanceDance.Utilities.Infrastructure.Models;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

public record GetPublishSasQuery(Guid VideoId) : IRequest<SharedBlob?>;
