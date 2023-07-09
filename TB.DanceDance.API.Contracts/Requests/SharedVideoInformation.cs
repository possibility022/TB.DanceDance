using System;
using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Requests
{

    public class SharedVideoInformation
    {
        [Required]
        [MaxLength(100)]
        [MinLength(5)]
        [RegularExpression("^[-^:) _a-zA-Z0-9]*$")]
        public string NameOfVideo { get; set; } = string.Empty;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public DateTime RecordedTimeUtc { get; set; } = DateTime.MinValue;

        [Required]
        public Guid? SharedWith { get; set; }

        [Required]
        public SharingWithType SharingWithType { get; set; }
    }

    public enum SharingWithType
    {
        NotSpecified,
        Group,
        Event
    }
}