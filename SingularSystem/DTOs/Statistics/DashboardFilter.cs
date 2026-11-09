namespace SingularSystem.DTOs.Statistics
{
    public class DashboardFilter
    {
        public string? Sex { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public Guid? SchoolID { get; set; }
        public int? MinimalAge { get; set; }
        public int? MaximumAge { get; set; }
        public string? RegistrationType { get; set; }
        public string? Disability { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }
}
