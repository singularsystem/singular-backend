using System.ComponentModel.DataAnnotations;

namespace SingularSystem.DTOs.Statistics
{
    public class MunicipalityStudentTargetRequest
    {
        [Required]
        public string State { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int TotalStudents { get; set; }
    }
}
