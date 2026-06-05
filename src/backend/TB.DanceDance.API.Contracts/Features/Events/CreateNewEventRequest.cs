using System.ComponentModel.DataAnnotations;
using TB.DanceDance.API.Contracts.Features.Events.Models;

namespace TB.DanceDance.API.Contracts.Features.Events
{
    public class CreateNewEventRequest
    {
        [Required]
        public EventModel Event { get; set; }
    }
}