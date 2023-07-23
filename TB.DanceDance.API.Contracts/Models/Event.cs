using System;
using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Models
{
    public class Event
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; }

        [Required]
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
