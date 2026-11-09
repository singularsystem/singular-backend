using System.ComponentModel.DataAnnotations;

namespace SingularSystem.DTOs.Files
{
    public class FileRequest
    {
        [Required]
        public IFormFile File { get; set; }
    }
}
