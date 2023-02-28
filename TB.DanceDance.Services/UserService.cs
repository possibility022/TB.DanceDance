using AspNetCore.Identity.MongoDbCore.Models;
using IdentityModel;
using MongoDB.Driver;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Identity;

namespace TB.DanceDance.Services
{
    public class UserService : IUserService
    {
        private readonly IMongoCollection<UserModel> usersCollection;
        private readonly IMongoCollection<Event> events;
        private readonly IMongoCollection<Group> groups;
        private readonly IMongoCollection<RequestedAssigment> requestedAssignment;

        public UserService(IMongoCollection<UserModel> usersCollection
        , IMongoCollection<Event> events
        , IMongoCollection<Group> groups
        , IMongoCollection<RequestedAssigment> requestedAssignment
            )
        {
            this.usersCollection = usersCollection;
            this.events = events;
            this.groups = groups;
            this.requestedAssignment = requestedAssignment;
        }

        public async Task<UserModel?> FindUserByNameAsync(string name)
        {
            var filter = new FilterDefinitionBuilder<UserModel>()
                .Eq(r => r.UserName, name);

            var res = await usersCollection.FindAsync(filter);
            return await res.FirstAsync();
        }

        public bool ValidateCredentials(string username, string password)
        {
            // todo migrate to asp net identity
            return usersCollection
                .Find(f => f.UserName == username && f.PasswordHash == password)
                .Any();
        }

        public Task AddUpsertUserAsync(UserModel model)
        {
            return usersCollection.ReplaceOneAsync(f => f.Id == model.Id, model, new ReplaceOptions()
            {
                IsUpsert = true
            });
        }

        public async Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroups(string userName)
        {
            if (userName is null)
                throw new ArgumentNullException(nameof(userName));

            List<Group> userGroups = null!;
            List<Event> userEvents = null!;

            var getGroups = this.groups.FindAsync(r => r.People.Contains(userName))
                    .ContinueWith(async r => userGroups = await r.Result.ToListAsync());

            var getEvents = this.events.FindAsync(r => r.Attenders.Contains(userName))
                .ContinueWith(async r => userEvents = await r.Result.ToListAsync());

            await Task.WhenAll(getGroups, getEvents);

            return (userGroups, userEvents);
        }

        public async Task<ICollection<Event>> GetAllEvents(string user)
        {
            var cursor = await this.events.FindAsync(FilterDefinition<Event>.Empty);
            var list = await cursor.ToListAsync();
            return list;
        }

        public async Task<ICollection<Group>> GetAllGroups(string user)
        {
            var cursor = await this.groups.FindAsync(FilterDefinition<Group>.Empty);
            var list = await cursor.ToListAsync();
            return list;
        }

        public async Task<IEnumerable<string>> GetUserVideosAssociationsIds(string userName)
        {
            (var userGroups, var userEvents) = await GetUserEventsAndGroups(userName);

            return userGroups.Select(r => r.Id)
                .Concat(userEvents.Select(e => e.Id));
        }

        public async Task<bool> UserIsAssociatedWith(string userName, string entityId)
        {
            var associatedTo = await GetUserVideosAssociationsIds(userName);
            return associatedTo.Any(r => r == entityId);
        }

        public async Task SaveEventsAssigmentRequest(string user, ICollection<string> events)
        {
            var toSave = events.Select(r => new RequestedAssigment()
            {
                AssignmentType = AssignmentType.Event,
                EntityId = r,
                UserId = user
            });

            await requestedAssignment.InsertManyAsync(toSave);
        }

        public async Task SaveGroupsAssigmentRequests(string user, ICollection<string> groups)
        {
            var toSave = groups.Select(r => new RequestedAssigment()
            {
                AssignmentType = AssignmentType.Group,
                EntityId = r,
                UserId = user
            });

            await requestedAssignment.InsertManyAsync(toSave);
        }
    }
}
