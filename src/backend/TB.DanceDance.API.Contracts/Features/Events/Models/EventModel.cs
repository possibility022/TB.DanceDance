using System;

namespace TB.DanceDance.API.Contracts.Features.Events.Models
{
    public class EventModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public DateTime Date { get; set; }
    }
}
