﻿using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Models
{
    public record SharedVideoInformation
    {
        [Required]
        [MaxLength(100)]
        [MinLength(5)]
        [RegularExpression("^[-^:) _a-zA-Z0-9]*$")]
        public string NameOfVideo { get; set; } = string.Empty;

        [Required]
        public DateTime RecordedTimeUtc { get; set; } = DateTime.MinValue;

        [Required]
        public SharingScopeModel SharedWith { get; set; } = null!;
    }
}