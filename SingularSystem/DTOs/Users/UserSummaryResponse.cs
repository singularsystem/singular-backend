using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;

namespace SingularSystem.DTOs.Users
{
    public class UserSummaryResponse
    {
        public string ID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string? State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public Guid? SchoolID { get; set; }
        public string? SchoolName { get; set; }
    }
}