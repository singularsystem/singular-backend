namespace SingularSystem.DTOs.PEIs
{
    public class PEISubjectResponse
    {
        public Guid ID { get; set; }
        public string Subject { get; set; }
        public ICollection<PEIResponseResponse> Responses { get; set; } = new List<PEIResponseResponse>();
        public Guid PEIID { get; set; }
    }
}
