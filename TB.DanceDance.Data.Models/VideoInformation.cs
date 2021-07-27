using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.Data.Models
{
    public class VideoInformation
    {

        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string BlobId { get; set; }
    }
}
