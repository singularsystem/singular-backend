using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.DTOs.PEIs
{
    public class PEIResponseRequest
    {
        public string Key { get; set; }
        public string Response { get; set; }
        public string? Category { get; set; }
    }
}
