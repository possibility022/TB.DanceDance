using System;
using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Models
{
    public class Event
    {
        public Guid Id { get; set; }

        [Required]
        [MinLength(5)]
        public string Name { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; }
    }
}
