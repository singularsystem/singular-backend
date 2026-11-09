using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.DTOs.PEIs
{
    public class PEIResponseResponse
    {
        public Guid ID { get; set; }
        public string Key { get; set; }
        public string Response { get; set; }
        public string? Category { get; set; }
        public Guid PEISubjectID { get; set; }
    }
}
