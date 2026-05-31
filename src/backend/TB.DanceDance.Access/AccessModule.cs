using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TB.DanceDance.Access.Authorization;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Events;
using TB.DanceDance.Access.Groups;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Access.Management;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access;

public static class AccessModule
{
    /// <summary>
    /// Registers every Access-module mediator handler. Handlers are never assembly-scanned —
    /// add new ones here.
    /// </summary>
    public static MediatorBuilder AddAccessModule(this MediatorBuilder builder)
    {
        return builder
            .Register<AddOrUpdateUserCommand, bool, AddOrUpdateUserHandler>()
            .Register<GetPendingUserRequestsQuery, UserRequests, GetPendingUserRequestsHandler>()
            .Register<SaveEventsAssignmentCommand, bool, SaveEventsAssignmentHandler>()
            .Register<SaveGroupsAssignmentCommand, bool, SaveGroupsAssignmentHandler>()
            .Register<GetAccessRequestsToApproveQuery, IReadOnlyCollection<RequestedAccess>, AccessRequestHandler>()
            .Register<ApproveAccessRequestCommand, bool, AccessRequestHandler>()
            .Register<DeclineAccessRequestCommand, bool, AccessRequestHandler>()
            .Register<CanUserUploadToEventRequest, bool, CanUserUpload>()
            .Register<CanUserUploadToGroupRequest, bool, CanUserUpload>()
            .Register<DoesUserHasAccessToSharedWith, bool, DoesUserHasAccessToSharedWithHandler>()
            .Register<GetUserGroupsAndEvents, UserGroupsAndEvents, GetUserGroupsAndEventsHandler>()
            .Register<CreateEventCommand, Guid, CreateEventCommandHandler>()
            .Register<GetAllEventsQuery, IReadOnlyCollection<EventDto>, GetAllEventsQueryHandler>()
            .Register<GetAllGroupsQuery, IReadOnlyCollection<GroupDto>, GetAllGroupsQueryHandler>()
            .Register<GetGroupByIdQuery, GroupDto?, GetGroupByIdQueryHandler>()
            .Register<GetUserGroupMembershipsQuery, IReadOnlyCollection<GroupMembershipDto>, GetUserGroupMembershipsQueryHandler>()
            .Register<GetUsersByIdsQuery, IReadOnlyCollection<UserInfoDto>, GetUsersByIdsHandler>();
    }

    /// <summary>
    /// Registers the Access module's <see cref="AccessDbContext"/> against the shared PostgreSQL
    /// database. One physical database; the context maps to the <c>access</c> (and default user)
    /// schema.
    /// </summary>
    public static IServiceCollection AddAccessModuleInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AccessDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
