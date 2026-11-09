using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(AppDbContext appDbContext, ILogger<FileStorageService> logger)
        {
            _appDbContext = appDbContext;
            _logger = logger;
        }

        public async Task<Guid> SaveFileInDatabase(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty or null.");

                const long maxFileSize = 10 * 1024 * 1024;
                if (file.Length > maxFileSize)
                {
                    throw new ArgumentException($"File size exceeds the limit of {maxFileSize / (1024 * 1024)} MB.");
                }

                var allowedContentTypes = new[] {
                    "application/pdf",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "image/jpeg",
                    "image/png"
                };

                if (!allowedContentTypes.Contains(file.ContentType))
                {
                    throw new ArgumentException($"File type '{file.ContentType}' is not allowed.");
                }

                byte[] fileContent;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileContent = memoryStream.ToArray();
                }

                var fileToBeSaved = new Models.Entities.File()
                {
                    OriginalFileName = file.FileName,
                    StoredFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName),
                    ContentType = file.ContentType,
                    Content = fileContent
                };

                await _appDbContext.Files.AddAsync(fileToBeSaved);
                await _appDbContext.SaveChangesAsync();

                return fileToBeSaved.ID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file to database.");
                return Guid.Empty;
            }
        }

        public async Task<List<Models.Entities.File>> GetAllFiles(int page, int pageSize)
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Min(pageSize, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var files = await _appDbContext.Files
                                               .AsNoTracking()
                                               .OrderBy(f => f.CreatedAt)
                                               .Skip(skip)
                                               .Take(pageSize)
                                               .ToListAsync();

                if (files == null || files.Count == 0)
                {
                    _logger.LogInformation("Não foi encontrado nenhum arquivo no banco");
                    return null!;
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files in database.");
                return null!;
            }
        }

        public async Task<Models.Entities.File> GetFileByFileName(string fileName)
        {
            try
            {
                var file = await _appDbContext.Files.FirstOrDefaultAsync(temp => temp.OriginalFileName.ToLower().Contains(fileName.ToLower()));

                if (file == null)
                {
                    _logger.LogInformation("Arquivo não foi encontrado");
                    return null!;
                }

                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file to database.");
                return null!;
            }
        }

        public async Task<Models.Entities.File> GetFileByID(Guid id)
        {
            try
            {
                var file = await _appDbContext.Files.FirstOrDefaultAsync(temp => temp.ID == id);

                if (file == null)
                {
                    _logger.LogInformation("Arquivo não foi encontrado");
                    return null!;
                }

                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file to database.");
                return null!;
            }
        }

        public async Task<List<Attachment>> GetFilesByStudent(Guid studentID)
        {
            try
            {
                var files = await _appDbContext.Attachments.Include(a => a.Student)
                    .Where(a => a.StudentID == studentID).ToListAsync();


                if (files == null || files.Count == 0)
                {
                    _logger.LogInformation("Não foi encontrado nenhum arquivo no banco");
                    return null!;
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file to database.");
                return null!;
            }
        }

        public async Task<bool> DeleteFile(Guid id)
        {
            try
            {
                var file = await _appDbContext.Files.FirstOrDefaultAsync(temp => temp.ID == id);

                if (file == null)
                {
                    _logger.LogInformation("Arquivo não foi encontrado");
                    return false;
                }

                _appDbContext.Files.Remove(file);
                await _appDbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file to database.");
                return false;
            }
        }


    }
}
