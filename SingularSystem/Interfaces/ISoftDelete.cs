namespace SingularSystem.Interfaces
{
    public interface ISoftDelete
    {
        bool IsActive { get; set; }
        DateTime? DeactivatedAt { get; set; }
    }
}
