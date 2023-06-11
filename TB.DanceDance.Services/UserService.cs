using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Data.PostgreSQL;
using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Services
{
    public class UserService : IUserService
    {
        private readonly DanceDbContext dbContext;

        public UserService(DanceDbContext dbContext
            )
        {
            this.dbContext = dbContext;
        }

        public IQueryable<Group> GetUserGroups(string userId)
        {
            var query = from assigments in dbContext.AssingedToGroups
                        join @group in dbContext.Groups on assigments.GroupId equals @group.Id
                        where assigments.UserId == userId
                        select @group;

            return query;
        }

        public IQueryable<Event> GetUserEvents(string userId)
        {
            var query = from assigments in dbContext.AssingedToEvents
                        join @event in dbContext.Events on assigments.EventId equals @event.Id
                        where assigments.UserId == userId
                        select @event;

            return query;
        }

        public (ICollection<Group>, ICollection<Event>) GetUserEventsAndGroups(string userId)
        {
            if (userId is null)
                throw new ArgumentNullException(nameof(userId));

            var userGroups = new HashSet<Group>();
            var userEvents = new HashSet<Event>();

            var query =
                from groupAssign in dbContext.AssingedToGroups
                join @group in dbContext.Groups on groupAssign.GroupId equals @group.Id
                where groupAssign.UserId == userId
                from eventAssign in dbContext.AssingedToEvents
                join @event in dbContext.Events on eventAssign.EventId equals @event.Id
                where eventAssign.UserId == userId
                select new { Group = @group, Event = @event };

            foreach (var res in query)
            {
                if (res.Group != null)
                    userGroups.Add(res.Group);

                if (res.Event != null)
                    userEvents.Add(res.Event);
            }

            return (userGroups, userEvents);
        }

        public async Task<ICollection<Event>> GetAllEvents(string user)
        {
            return await dbContext.Events.ToListAsync();
        }

        public async Task<ICollection<Group>> GetAllGroups(string user)
        {
            return await dbContext.Groups.ToListAsync();
        }

        public async Task SaveEventsAssigmentRequest(string user, ICollection<Guid> events)
        {
            var toSave = events.Select(@event => new EventAssigmentRequest()
            {
                EventId = @event,
                UserId = user
            });

            dbContext.EventAssigmentRequests.AddRange(toSave);
            await dbContext.SaveChangesAsync();
        }

        public async Task SaveGroupsAssigmentRequests(string user, ICollection<Guid> groups)
        {
            var toSave = groups.Select(group => new GroupAssigmentRequest()
            {
                GroupId = group,
                UserId = user
            });

            dbContext.GroupAssigmentRequests.AddRange(toSave);
            await dbContext.SaveChangesAsync();
        }
    }
}
