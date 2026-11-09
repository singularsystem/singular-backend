namespace SingularSystem.DTOs.Portage
{
    public class PortageRequest
    {
        public Guid? StudentID { get; set; }
        public Guid? UserID { get; set; }
        public string AgeRange { get; set; }
        public Dictionary<string, PortageValueRequest> Responses { get; set; }
    }
}
