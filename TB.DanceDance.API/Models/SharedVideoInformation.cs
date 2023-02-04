using System.ComponentModel.DataAnnotations;
using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.API.Models
{
    public record SharedVideoInformation
    {
        [Required]
        [MaxLength(100)]
        [MinLength(50)]
        public string NameOfVideo { get; set; } = string.Empty;

        [Required]
        public DateTime RecordedTimeUtc { get; set; } = DateTime.MinValue;

        [Required]
        public SharingScope SharedWith { get; set; } = null!;
    }
}
