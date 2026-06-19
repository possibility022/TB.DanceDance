using Application.Extensions;
using FastEndpoints;
using FluentValidation;
using TB.DanceDance.API.Contracts.Features.Groups;

namespace Application.Features.Groups.Endpoints;

public class CreateGroupValidator : Validator<CreateGroupRequest>
{
    public CreateGroupValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MinimumLength(3).WithMessage("Group name must be at least 3 characters long.");

        RuleFor(x => x.SeasonStart)
            .NotEmpty().WithMessage("Season start is required.");

        RuleFor(x => x.SeasonEnd)
            .NotEmpty().WithMessage("Season end is required.");

        RuleFor(x => x)
            .Must(x => x.SeasonStart <= x.SeasonEnd)
            .WithMessage("Season start must be on or before season end.");
    }
}

public class CreateGroupEndpoint : Endpoint<CreateGroupRequest, CreateGroupResponse>
{
    private readonly IGroupService groupService;

    public CreateGroupEndpoint(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Groups.Create);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CreateGroupRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();

        var group = await groupService.CreateGroupAsync(
            req.Name,
            DateOnly.FromDateTime(req.SeasonStart),
            DateOnly.FromDateTime(req.SeasonEnd),
            userId,
            ct);

        await Send.OkAsync(new CreateGroupResponse { Id = group.Id }, ct);
    }
}
