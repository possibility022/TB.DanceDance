using Application.Features.Groups.Models;

namespace Application.Features.Groups.Endpoints;

public record ListGroupVideosResponse
{
    public required VideoFromGroupInformation[] Videos { get; init; }
}