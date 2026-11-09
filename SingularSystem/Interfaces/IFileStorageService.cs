using SingularSystem.Models.Entities;
namespace SingularSystem.Interfaces
{
    public interface IFileStorageService
    {
        Task<Guid> SaveFileInDatabase(IFormFile file);
        Task<List<Models.Entities.File>> GetAllFiles(int page, int pageSize);
        Task<Models.Entities.File> GetFileByFileName(string fileName);
        Task<Models.Entities.File> GetFileByID(Guid id);
        Task<List<Attachment>> GetFilesByStudent(Guid studentID);
        Task<bool> DeleteFile(Guid id);
    }
}
