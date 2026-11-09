namespace SingularSystem.DTOs.Attachments
{
    public class AttachmentRequest
    {
        public Guid StudentID { get; set; }
        public List<IFormFile> Files { get; set; }
    }
}
