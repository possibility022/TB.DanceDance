using System;

namespace TB.DanceDance.API.Contracts
{
    public class Event
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Date { get; set; }
        public EventType Type { get; set; }
    }

    public enum EventType
    {
        Unknown = 0,
        PointedEvent,
        MediumNotPointed,
        SmallWorkshop
    }

    public class SharedWith
    {
        public Guid Id { get; set; }
        public Guid VideoId { get; set; }
        public string UserId { get; set; } = null!;
        public Guid? EventId { get; set; }
        public Guid? GroupId { get; set; }
    }
}
