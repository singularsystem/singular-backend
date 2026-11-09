using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class Attachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(Student))]
        public Guid? StudentID { get; set; }
        public Student? Student { get; set; }

        public string FileName { get; set; }
        public string S3Key { get; set; }
        public string S3Bucket { get; set; }
        public string Url { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

    }
}
