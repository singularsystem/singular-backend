using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using SingularSystem.DTOs.Attachments;
using SingularSystem.DTOs.Files;
using SingularSystem.Interfaces;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FilesController : BaseController
    {
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<FilesController> _logger;
        private readonly IFileStorageService _fileStorageService;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _bucketName;
        private readonly IConfiguration _configuration;


        public FilesController(AppDbContext appDbContext, ILogger<FilesController> logger, IFileStorageService fileStorageService, IConfiguration configuration)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _accessKey = configuration["S3BucketAccessKey"]!;
            _secretKey = configuration["S3BucketSecretKey"]!;
            _bucketName = configuration["S3BucketName"]!;
            _configuration = configuration;

        }

        private string GeneratePreSignedURL(string bucketName, string key, double durationMinutes = 5)
        {
            using var s3Client = S3ClientFactory.CreateClient(_configuration);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(durationMinutes)
            };

            string url = s3Client.GetPreSignedURL(request);
            return url;
        }

        [Authorize(Policy = "AdminOrSecretary")]
        [HttpPost]
        public async Task<IActionResult> UploadFiles([FromForm] AttachmentRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem();

                if (model.Files == null || model.Files.Count == 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Message = "Nenhum arquivo enviado",
                        Success = false
                    });
                }

                using var s3Client = S3ClientFactory.CreateClient(_configuration);
                var fileTransferUtility = new TransferUtility(s3Client);

                var attachments = new List<Models.Entities.Attachment>();

                foreach (var file in model.Files)
                {
                    if (file.Length == 0) continue;

                    var fileKey = $"{Guid.NewGuid()}_{file.FileName}";

                    using var stream = file.OpenReadStream();
                    await fileTransferUtility.UploadAsync(stream, _bucketName, fileKey);

                    var fileUrl = $"https://{_bucketName}.s3.amazonaws.com/{fileKey}";

                    attachments.Add(new Models.Entities.Attachment
                    {
                        StudentID = model.StudentID,
                        FileName = file.FileName,
                        S3Key = fileKey,
                        S3Bucket = _bucketName,
                        Url = fileUrl
                    });
                }

                if (attachments.Count > 0)
                {
                    _appDbContext.Attachments.AddRange(attachments);
                    await _appDbContext.SaveChangesAsync();
                }

                return StatusCode(201, new ApiResponse<object>
                {
                    Success = true,
                    Message = $"{attachments.Count} arquivo(s) salvo(s) com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar arquivo");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erro ao enviar arquivo"
                });
            }
        }

        [Authorize(Policy = "AdminOrSchool")]
        [HttpPost("students")]
        public async Task<IActionResult> UploadStudentFiles([FromForm] AttachmentRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem();

                if (model.Files == null || model.Files.Count == 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Message = "Nenhum arquivo enviado",
                        Success = false
                    });
                }

                using var s3Client = S3ClientFactory.CreateClient(_configuration);
                var fileTransferUtility = new TransferUtility(s3Client);

                var attachments = new List<Models.Entities.Attachment>();

                foreach (var file in model.Files)
                {
                    if (file.Length == 0) continue;

                    var fileKey = $"{model.StudentID}/{Guid.NewGuid()}_{file.FileName}";

                    using var stream = file.OpenReadStream();
                    await fileTransferUtility.UploadAsync(stream, _bucketName, fileKey);

                    var fileUrl = $"https://{_bucketName}.s3.amazonaws.com/{fileKey}";

                    attachments.Add(new Models.Entities.Attachment
                    {
                        StudentID = model.StudentID,
                        FileName = file.FileName,
                        S3Key = fileKey,
                        S3Bucket = _bucketName,
                        Url = fileUrl
                    });
                }

                if (attachments.Count > 0)
                {
                    _appDbContext.Attachments.AddRange(attachments);
                    await _appDbContext.SaveChangesAsync();
                }

                return StatusCode(201, new ApiResponse<object>
                {
                    Success = true,
                    Message = $"{attachments.Count} arquivo(s) salvo(s) com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar arquivo");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erro ao enviar arquivo"
                });
            }
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet]
        public async Task<IActionResult> GetFiles([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var files = await _fileStorageService.GetAllFiles(page, pageSize);

                if (files == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Nenhum arquivo foi encontrado"
                    });
                }

                var response = new List<FileResponse>();

                foreach (var file in files)
                {
                    var fileToReturn = new FileResponse()
                    {
                        ID = file.ID,
                        OriginalFileName = file.OriginalFileName,
                        StoredFileName = file.StoredFileName,
                        ContentType = file.ContentType,
                        Content = file.Content,
                        CreatedAt = file.CreatedAt,
                        UpdatedAt = file.UpdatedAt,
                    };

                    response.Add(fileToReturn);
                }

                return Ok(new ApiResponse<List<FileResponse>>()
                {
                    Success = true,
                    Message = "Lista de arquivos retornada com sucesso",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar arquivos");
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = "Erro ao buscar arquivos"
                });
            }
        }

        [Authorize]
        [HttpGet("students/{studentID}")]
        public async Task<IActionResult> GetStudentFiles(Guid studentID)
        {
            try
            {
                var student = await _appDbContext.Students.IgnoreQueryFilters().AsNoTracking()
                    .Include(s => s.School)
                    .FirstOrDefaultAsync(s => s.ID == studentID);

                if (student == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Estudante não encontrado."
                    });
                }

                var userManager = HttpContext?.RequestServices?.GetService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
                var user = userManager != null ? await userManager.FindByIdAsync(CurrentUserId) : null;
                if (user == null) return Unauthorized();
                var roles = await userManager.GetRolesAsync(user);

                if (!HasStudentAccess(user, student, roles))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, DocumentAccessDenied.Body());
                }

                var files = await _fileStorageService.GetFilesByStudent(studentID);

                if (files == null)
                {
                    return Ok(new ApiResponse<object>()
                    {
                        Success = true,
                        Message = "Nenhum arquivo foi encontrado"
                    });
                }

                var response = new List<AttachmentResponse>();

                foreach (var file in files)
                {
                    var url = GeneratePreSignedURL(file.S3Bucket, file.S3Key);
                    var fileToReturn = new AttachmentResponse()
                    {
                        ID = file.ID,
                        FileName = file.FileName,
                        S3Bucket = file.S3Bucket,
                        S3Key = file.S3Key,
                        Url = url,
                        StudentID = file.StudentID,
                        CreatedAt = file.CreatedAt,
                        UpdatedAt = file.UpdatedAt,
                    };

                    response.Add(fileToReturn);
                }

                return Ok(new ApiResponse<List<AttachmentResponse>>()
                {
                    Success = true,
                    Message = "Lista de arquivos retornada com sucesso",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar arquivos");
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = "Erro ao buscar arquivos"
                });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFile(Guid id)
        {
            try
            {
                var file = await _fileStorageService.GetFileByID(id);

                if (file == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Nenhum arquivo foi encontrado com ID {id}"
                    });
                }

                var fileResponse = new FileResponse()
                {
                    ID = file.ID,
                    OriginalFileName = file.OriginalFileName,
                    StoredFileName = file.StoredFileName,
                    ContentType = file.ContentType,
                    Content = file.Content,
                    CreatedAt = file.CreatedAt,
                    UpdatedAt = file.UpdatedAt,
                };

                return Ok(new ApiResponse<FileResponse>()
                {
                    Success = true,
                    Message = "Arquivo retornado retornada com sucesso",
                    Data = fileResponse
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar arquivo");
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = "Erro ao buscar arquivo"
                });
            }
        }

        [Authorize]
        [HttpGet("name={name}")]
        public async Task<IActionResult> GetFile(string name)
        {
            try
            {
                var file = await _fileStorageService.GetFileByFileName(name);

                if (file == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Nenhum arquivo foi encontrado contendo {name}"
                    });
                }

                var fileResponse = new FileResponse()
                {
                    ID = file.ID,
                    OriginalFileName = file.OriginalFileName,
                    StoredFileName = file.StoredFileName,
                    ContentType = file.ContentType,
                    Content = file.Content,
                    CreatedAt = file.CreatedAt,
                    UpdatedAt = file.UpdatedAt,
                };

                return Ok(new ApiResponse<FileResponse>()
                {
                    Success = true,
                    Message = "Arquivo retornado com sucesso",
                    Data = fileResponse
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar arquivo");
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = "Erro ao buscar arquivo"
                });
            }
        }

        [Authorize(Policy = "AdminOrSchool")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(Guid id, [FromQuery] bool confirmDeletion)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                if (!confirmDeletion)
                {
                    return BadRequest(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "A confirmação da exclusão é obrigatória"
                    });
                }

                var attachment = await _appDbContext.Attachments.FirstOrDefaultAsync(temp => temp.ID == id);

                if (attachment == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Message = $"Arquivo não encontrado com ID {id}",
                        Success = false
                    });
                }

                using var s3Client = S3ClientFactory.CreateClient(_configuration);

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = attachment.S3Bucket,
                    Key = attachment.S3Key
                };

                var response = await s3Client.DeleteObjectAsync(deleteRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _appDbContext.Attachments.Remove(attachment);
                    await _appDbContext.SaveChangesAsync();

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Arquivo excluído com sucesso"
                    });
                }

                return BadRequest(new ApiResponse<Object>()
                {
                    Success = false,
                    Message = "Erro ao excluir arquivo"
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar arquivo");
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = "Erro ao excluir arquivo"
                });
            }
        }
    }
}
