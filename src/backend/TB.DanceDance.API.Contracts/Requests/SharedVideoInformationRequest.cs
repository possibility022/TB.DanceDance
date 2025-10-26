using System;
using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Requests
{

    public class SharedVideoInformationRequest
    {
        [Required]
        [MaxLength(100)]
        [MinLength(5)]
        public string NameOfVideo { get; set; } = string.Empty;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public DateTime RecordedTimeUtc { get; set; } = DateTime.MinValue;

        [Required]
        public Guid? SharedWith { get; set; }

        [Required]
        public SharingWithType SharingWithType { get; set; }
        
        /// <summary>
        /// VideoId of an existing upload to resume. Provide this when you want to get a SAS URL for a previously created blob that was partially
        /// uploaded, and you want to continue the upload.
        /// </summary>
        public Guid? VideoId { get; set; }
    }

    public enum SharingWithType
    {
        NotSpecified,
        Group,
        Event
    }
}