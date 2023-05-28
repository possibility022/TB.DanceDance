using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Data.PostgreSQL;
using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.VideoLoader
{
    class DbMigration
    {
        private readonly DanceDbContext context;
        private readonly IMongoCollection<VideoInformation> videosMongo;
        private readonly IMongoCollection<Data.MongoDb.Models.Group> groups;
        private readonly IMongoCollection<Data.MongoDb.Models.Event> events;

        public DbMigration(DanceDbContext context,
            IMongoCollection<VideoInformation> videosMongo,
            IMongoCollection<TB.DanceDance.Data.MongoDb.Models.Group> groups,
            IMongoCollection<TB.DanceDance.Data.MongoDb.Models.Event> events

            )
        {
            this.context = context;
            this.videosMongo = videosMongo;
            this.groups = groups;
            this.events = events;
        }

        private async Task MigrateVideosAsync()
        {
            var cursor = videosMongo.Find(FilterDefinition<VideoInformation>.Empty);
            var viedos = await cursor.ToListAsync();

            foreach (var v in viedos)
            {
                context.Add(new Video()
                {
                    BlobId = v.BlobId,
                    Duration = v.Duration,
                    RecordedDateTime = SetKindUtc(v.RecordedTimeUtc),
                    SharedDateTime = SetKindUtc(v.SharedDateTimeUtc),
                    UploadedBy = "Tomek",
                    MetadataAsJson = v.MetadataAsJson,
                    Name = v.Name,
                });
            }

            context.SaveChanges();
        }

        public static DateTime? SetKindUtc(DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                return SetKindUtc(dateTime.Value);
            }
            else
            {
                return null;
            }
        }
        public static DateTime SetKindUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc) { return dateTime; }
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        private async Task MigrateEventsAndGroups()
        {
            var cursor = await events.FindAsync(FilterDefinition<Data.MongoDb.Models.Event>.Empty);
            var allEvents = await cursor.ToListAsync();

            foreach (var e in allEvents)
            {
                var entity = new Data.PostgreSQL.Models.Event()
                {
                    Date = SetKindUtc(e.Date.DateTime),
                    Name = e.Name,
                    Type = MapEvent(e.EventType)
                };

                context.Add(entity);
            }

            var groupsCursor = await groups.FindAsync(FilterDefinition<Data.MongoDb.Models.Group>.Empty);
            var allGroups = await groupsCursor.ToListAsync();

            foreach (var e in allGroups)
            {
                context.Add(new Data.PostgreSQL.Models.Group()
                {
                    Name = e.GroupName,
                });
            }

            context.SaveChanges();
        }

        private async Task MigrateAssigments()
        {
            var cursor = videosMongo.Find(FilterDefinition<VideoInformation>.Empty);
            var viedos = await cursor.ToListAsync();

            var eventCursor = await events.FindAsync(FilterDefinition<Data.MongoDb.Models.Event>.Empty);
            var allEvents = eventCursor.ToList().ToDictionary(r => r.Id);

            var groupsCursor = await groups.FindAsync(FilterDefinition<Data.MongoDb.Models.Group>.Empty);
            var allGroups = groupsCursor.ToList().ToDictionary(r => r.Id);

            var allNewVideos = context.Videos.ToDictionary(r => r.Name);
            var allNewEvents = context.Events.ToDictionary(r => r.Name);
            var allNewGroups = context.Groups.ToDictionary(r => r.Name);

            foreach (var v in viedos)
            {
                var sharedWith = new SharedWith()
                {
                    UserId = "f98c2a17-1577-481f-9a7b-605825763bb4",
                    VideoId = allNewVideos[v.Name].Id
                };

                switch (v.SharedWith.Assignment)
                {
                    case AssignmentType.Person:
                        break;

                    case AssignmentType.Event:
                        var oldEventName = allEvents[v.SharedWith.EntityId].Name;
                        sharedWith.EventId = allNewEvents[oldEventName].Id;
                        break;

                    case AssignmentType.Group:
                        var oldGroupName = allGroups[v.SharedWith.EntityId].GroupName;
                        sharedWith.GroupId = allNewGroups[oldGroupName].Id;
                        break;

                    case AssignmentType.NotSpecified:
                        throw new NotSupportedException();

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                context.Add(sharedWith);
            }

            context.SaveChanges();
        }

        private async Task MigrateUsersAssigments()
        {
            var eventCursor = await events.FindAsync(FilterDefinition<Data.MongoDb.Models.Event>.Empty);
            var allEvents = eventCursor.ToList().ToDictionary(r => r.Id);

            var groupsCursor = await groups.FindAsync(FilterDefinition<Data.MongoDb.Models.Group>.Empty);
            var allGroups = groupsCursor.ToList().ToDictionary(r => r.Id);

            var allNewEvents = context.Events.ToDictionary(r => r.Name);
            var allNewGroups = context.Groups.ToDictionary(r => r.Name);

            foreach(var group in allGroups)
            {
                foreach(var userId in group.Value.People)
                {
                    context.Add(new AssignedToGroup()
                    {
                        UserId = userId,
                        GroupId = allNewGroups[group.Value.GroupName].Id
                    });
                }
            }


            foreach (var e in allEvents)
            {
                foreach (var userId in e.Value.Attenders)
                {
                    context.Add(new AssignedToEvent()
                    {
                        UserId = userId,
                        EventId = allNewEvents[e.Value.Name].Id
                    });
                }
            }

            context.SaveChanges();
        }

        private Data.PostgreSQL.Models.EventType MapEvent(Data.MongoDb.Models.EventType eventType)
        {
            switch (eventType)
            {
                case Data.MongoDb.Models.EventType.Unknown:
                    return Data.PostgreSQL.Models.EventType.Unknown;

                case Data.MongoDb.Models.EventType.MediumNotPointed:
                    return Data.PostgreSQL.Models.EventType.MediumNotPointed;

                case Data.MongoDb.Models.EventType.SmallWorkshop:
                    return Data.PostgreSQL.Models.EventType.SmallWorkshop;

                case Data.MongoDb.Models.EventType.PointedEvent:
                    return Data.PostgreSQL.Models.EventType.PointedEvent;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task MigrateAsync()
        {
            await MigrateVideosAsync();
            await MigrateEventsAndGroups();
            await MigrateAssigments();
            await MigrateUsersAssigments();
        }
    }
}
