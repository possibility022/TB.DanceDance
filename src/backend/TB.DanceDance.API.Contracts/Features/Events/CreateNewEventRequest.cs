using System.ComponentModel.DataAnnotations;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Features.Events
{
    public class CreateNewEventRequest
    {
        [Required]
        public Event Event { get; set; }
    }
}
