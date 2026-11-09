using SingularSystem.DTOs.Students;
using SingularSystem.Mappers;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SingularSystem.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class StudentsController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(AppDbContext appDbContext, ILogger<StudentsController> logger, NotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        private async Task<IQueryable<Student>> BuildStudentQuery(
            ApplicationUser currentUser,
            string state,
            string city,
            string schoolID,
            string searchTerm,
            string serie = "")
        {
            var roles = await _userManager.GetRolesAsync(currentUser);
            var isAdmin = roles.Contains("Admin");

            var query = _appDbContext.Students.Include(s => s.School)
                                              .AsNoTracking()
                                              .AsSplitQuery();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(s => s != null &&
                    s.FullName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(s => s.State == state || (s.School != null && s.School.State == state));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(s => s.City == city || (s.School != null && s.School.City == city));
            }

            if (!string.IsNullOrWhiteSpace(schoolID))
            {
                query = query.Where(s => s.SchoolID == Guid.Parse(schoolID));
            }

            if (!string.IsNullOrWhiteSpace(serie))
            {
                var serieEnum = Helpers.GetEnumValueFromDisplayName<Serie>(serie);
                if (serieEnum != Serie.None)
                {
                    query = query.Where(s => s.Serie == serieEnum);
                }
            }

            if (!isAdmin)
            {
                if (roles.Contains("Coordinator"))
                {
                    query = query.Where(s => (s.City == currentUser.City && s.State == currentUser.State) ||
                                             (s.School != null && s.School.City == currentUser.City && s.School.State == currentUser.State));
                }
                else if (roles.Contains("Principal"))
                {
                    query = query.Where(s => (s.City == currentUser.City && s.State == currentUser.State) ||
                                             (s.School != null && s.School.City == currentUser.City && s.School.State == currentUser.State));
                }
                else if (roles.Contains("Psychologist") || roles.Contains("SocialWork") ||
                         roles.Contains("MunicipalManager") || roles.Contains("Secretary") ||
                         roles.Contains("Psychopedagogue") || roles.Contains("Teacher"))
                {
                    query = query.Where(s => (s.City == currentUser.City && s.State == currentUser.State) ||
                                             (s.School != null && s.School.City == currentUser.City && s.School.State == currentUser.State) ||
                                             (currentUser.SchoolID != null && s.SchoolID == currentUser.SchoolID));
                }
                else
                {
                    // Default security filter for any other role that is not Admin
                    query = query.Where(s => (s.City == currentUser.City && s.State == currentUser.State) ||
                                             (s.School != null && s.School.City == currentUser.City && s.School.State == currentUser.State));
                }
            }

            return query;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] StudentRequest model)
        {
            try
            {
                _logger.LogInformation("Recebida solicitação para criar estudante: {@Model}", model);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("Falha na validação do modelo ao criar estudante: {Errors}. Model: {@Model}", string.Join(", ", errors), model);
                    return BadRequest(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Dados inválidos: " + string.Join("; ", errors)
                    });
                }

                var school = await _appDbContext.Schools.FirstOrDefaultAsync(temp => temp.City == model.SchoolCity && temp.State == model.SchoolState && temp.Name == model.SchoolName);

                if (school == null)
                {
                    _logger.LogWarning("Escola não encontrada para o estudante. Dados: {SchoolName}, {City}, {State}", model.SchoolName, model.SchoolCity, model.SchoolState);
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Escola '{model.SchoolName}' na cidade '{model.SchoolCity}' não encontrada"
                    });
                }

                var student = StudentMapper.ToEntity(model, school);

                await _appDbContext.Students.AddAsync(student);
                await _appDbContext.SaveChangesAsync();

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Aluno criado com sucesso com ID {student.ID}",
                    Source = "StudentsController",
                    UserID = CurrentUserGuid,
                    EntityType = "Student",
                    EntityID = student.ID,
                    Details = "",
                    Action = "Create"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Novo aluno criado!",
                    message: "Um novo aluno foi criado.",
                    actionURL: "api/v1/students"
                );

                var roles = new[] { "SocialWork", "Psychologist", "Psychopedagogue" };

                var recipients = new List<ApplicationUser>();

                foreach (var role in roles)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                    var filteredUsers = usersInRole
                        .Where(u => u.City == student.City && u.State == student.State)
                        .ToList();

                    recipients.AddRange(filteredUsers);
                }

                var uniqueRecipients = recipients
                    .GroupBy(u => u.Id)
                    .Select(g => g.First())
                    .ToList();

                foreach (var recipient in uniqueRecipients)
                {
                    await _notificationService.CreateNotification(
                        userId: recipient.Id,
                        type: TypeNotification.System,
                        title: "Novo aluno criado!",
                        message: "Um novo aluno está disponível.",
                        actionURL: "api/v1/students"
                    );
                }

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Estudante criado com sucesso",
                    Data = new
                    {
                        student.ID
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar estudante");
                
                string message = "Ocorreu um erro ao criar estudante";
                if (ex is ArgumentException)
                {
                    message = ex.Message;
                }
                else if (ex is UnauthorizedAccessException)
                {
                    message = ex.Message;
                }
                else if (ex is DbUpdateException dbEx)
                {
                    // Tenta pegar a mensagem da exceção interna (geralmente onde está o erro do banco)
                    message = dbEx.InnerException?.Message ?? dbEx.Message;
                    
                    if (message.Contains("IX_Students_CPF") || message.Contains("unique index 'CPF'"))
                        message = "Já existe um estudante cadastrado com este CPF.";
                    else if (message.Contains("IX_Students_SUSCard") || message.Contains("unique index 'SUSCard'"))
                        message = "Já existe um estudante cadastrado com este Cartão SUS.";
                    else if (message.Contains("IX_Students_RG") || message.Contains("unique index 'RG'"))
                        message = "Já existe um estudante cadastrado com este RG.";
                }

                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = message
                });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetStudents([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] string serie = "")
        {
            try
            {
                const int maxPageSize = 50;
                var currentYear = DateTime.UtcNow.Year;
                pageSize = Math.Min(pageSize, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var user = await _userManager.FindByIdAsync(CurrentUserId);

                if (user == null)
                {
                    return Unauthorized();
                }
                
                var query = await BuildStudentQuery(user, state, city, schoolID, searchTerm, serie);

                if (!query.Any())
                {
                    return Ok(new ApiResponse<List<StudentResponse>>
                    {
                        Success = true,
                        Message = "Nenhum estudante foi cadastrado ainda"
                    });
                }

                var totalStudents = await query.CountAsync();
                var students = await query.OrderByDescending(f => f.CreatedAt)
                                          .Skip(skip)
                                          .Take(pageSize)
                                          .AsSplitQuery()
                                          .ToListAsync();

                var response = students.Select(StudentMapper.ToSummaryResponse).ToList();

                return Ok(new ApiResponse<List<StudentSummaryResponse>>
                {
                    Success = true,
                    Message = "Lista de estudantes retornada com sucesso",
                    Data = response,
                    Total = totalStudents
                });

            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao retornar estudantes. {ex.Message}"
                });
            }
        }

        [Authorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllStudentsNoPaging([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] string serie = "")
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);

                if (user == null)
                {
                    return Unauthorized();
                }

                var query = await BuildStudentQuery(user, state, city, schoolID, searchTerm, serie);

                if (!query.Any())
                {
                    return Ok(new ApiResponse<List<StudentResponse>>
                    {
                        Success = true,
                        Message = "Nenhum estudante foi cadastrado ainda"
                    });
                }

                var totalStudents = await query.CountAsync();
                var students = await query.OrderByDescending(f => f.CreatedAt)
                                          .AsSplitQuery()
                                          .ToListAsync();

                var responses = students.Select(StudentMapper.ToSummaryResponse).ToList();

                return Ok(new ApiResponse<List<StudentSummaryResponse>>
                {
                    Success = true,
                    Message = "Lista de estudantes retornada com sucesso",
                    Data = responses,
                    Total = students.Count
                });

            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao retornar estudantes. {ex.Message}"
                });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudent(Guid id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);

                var student = await _appDbContext.Students.Include(s => s.School)
                                                          .AsSplitQuery()
                                                          .FirstOrDefaultAsync(temp => temp.ID == id);

                if (student == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Estudante com ID {id} não encontrado"
                    });
                }

                // Security check
                if (!HasStudentAccess(user, student, roles))
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Sem permissão para acessar este estudante."
                    });
                }

                var studentResponse = StudentMapper.ToResponse(student);

                return Ok(new ApiResponse<StudentResponse>()
                {
                    Success = true,
                    Message = "Estudante retornado com sucesso",
                    Data = studentResponse
                });

            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao retornar estudante. {ex.Message}"
                });
            }
        }

        [Authorize]
        [HttpGet("{id}/documents")]
        public async Task<IActionResult> GetStudentDocuments(Guid id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);

                var studentData = await _appDbContext.Students.AsNoTracking()
                                    .Where(s => s.ID == id)
                                    .Select(s => new
                                    {
                                        Student = s,
                                        School = s.School,
                                        Documents = s.BaseDocuments
                                            .OrderByDescending(d => d.CreatedAt)
                                            .Select(d => StudentMapper.ToDocumentResponse(d))
                                            .ToList()
                                    })
                                    .FirstOrDefaultAsync();

                if (studentData == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Estudante com ID {id} não encontrado"
                    });
                }

                // Security check
                if (!HasStudentAccess(user, studentData.Student, roles))
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Sem permissão para acessar os documentos deste estudante."
                    });
                }

                return Ok(new ApiResponse<List<StudentDocumentResponse>>()
                {
                    Success = true,
                    Message = "Estudante retornado com sucesso",
                    Data = studentData.Documents
                });

            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao retornar estudante. {ex.Message}"
                });
            }
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateStudent([FromBody] StudentUpdateRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);

                var student = await _appDbContext.Students.Include(s => s.School).FirstOrDefaultAsync(temp => temp.ID == model.ID);

                if (student == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Estudante com ID {model.ID} não foi encontrado"
                    });
                }

                // Security check
                if (!HasStudentAccess(user, student, roles)) return Forbid();

                var school = await _appDbContext.Schools.FirstOrDefaultAsync(temp => temp.Name == model.SchoolName && temp.City == model.SchoolCity && temp.State == model.SchoolState);

                if (school == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Escola com Nome {model.SchoolName} na Cidade {model.SchoolCity} não foi encontrada"
                    });
                }

                StudentMapper.UpdateEntity(student, model, school);

                _appDbContext.Students.Update(student);
                await _appDbContext.SaveChangesAsync();

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Aluno atualizado com sucesso com ID {student.ID}",
                    Source = "StudentsController",
                    UserID = CurrentUserGuid,
                    EntityType = "Student",
                    EntityID = student.ID,
                    Details = "",
                    Action = "Update"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: $"Aluno com ID {student.ID} atualizado",
                    message: "Um aluno foi atualizado com sucesso.",
                    actionURL: "api/v1/students"
                );

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Estudante atualiado com sucesso",
                    Data = new
                    {
                        student.ID,
                        student.FullName,
                        student.DateOfBirth,
                        student.Address,
                        student.NameOfPersonResponsible,
                        student.SUSCard,
                        student.RG,
                        student.CPF
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar dados de estudante");
                
                string message = "Ocorreu um erro ao atualizar o estudante";
                if (ex is ArgumentException)
                {
                    message = ex.Message;
                }
                else if (ex is DbUpdateException dbEx)
                {
                    message = dbEx.InnerException?.Message ?? dbEx.Message;
                    
                    if (message.Contains("IX_Students_CPF") || message.Contains("unique index 'CPF'"))
                        message = "Já existe um estudante cadastrado com este CPF.";
                    else if (message.Contains("IX_Students_SUSCard") || message.Contains("unique index 'SUSCard'"))
                        message = "Já existe um estudante cadastrado com este Cartão SUS.";
                    else if (message.Contains("IX_Students_RG") || message.Contains("unique index 'RG'"))
                        message = "Já existe um estudante cadastrado com este RG.";
                }

                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = message
                });
            }
        }

        //[Authorize(Roles = "Admin,Secretary,Principal,Coordinator")]
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateStudent(Guid id)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);

                var student = await _appDbContext.Students.Include(s => s.School).IgnoreQueryFilters().FirstOrDefaultAsync(temp => temp.ID == id);

                if (student == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Estudante com ID {id} não foi encontrado"
                    });
                }

                // Security check
                if (!HasStudentAccess(user, student, roles)) return Forbid();

                if (!student.IsActive)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Este estudante já está desativado"
                    });
                }

                student.IsActive = false;
                student.DeactivatedAt = DateTime.UtcNow;

                _appDbContext.Students.Update(student);
                await _appDbContext.SaveChangesAsync();

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Aluno desativado com sucesso com ID {student.ID}",
                    Source = "StudentsController",
                    UserID = CurrentUserGuid,
                    EntityType = "Student",
                    EntityID = student.ID,
                    Details = "",
                    Action = "Deactivate"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: $"Aluno com ID {student.ID} foi desativado",
                    message: "Um aluno foi desativado com sucesso.",
                    actionURL: "api/v1/students"
                );

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Estudante desativado com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao desativar estudante com id {id}");
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                });
            }
        }

        private sealed class StudentImportSpreadsheetRow
        {
            public string FullName { get; set; } = string.Empty;
            public string Sex { get; set; } = string.Empty;
            public string DateOfBirth { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public string SchoolName { get; set; } = string.Empty;
            public string SchoolCity { get; set; } = string.Empty;
            public string SchoolState { get; set; } = string.Empty;
            public string Serie { get; set; } = string.Empty;
            public string NameOfPersonResponsible { get; set; } = string.Empty;
            public string MaritalStatusParents { get; set; } = string.Empty;
            public string SkinColor { get; set; } = string.Empty;
            public string? SUSCard { get; set; }
            public string? RG { get; set; }
            public string? CPF { get; set; }
        }

        private sealed class StudentImportSuccessItem
        {
            public int RowNumber { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string SchoolName { get; set; } = string.Empty;
        }

        private sealed class StudentImportFailureItem
        {
            public int RowNumber { get; set; }
            public string FullName { get; set; } = string.Empty;
            public List<string> Errors { get; set; } = new();
            public StudentImportSpreadsheetRow RowData { get; set; } = new();
        }

        [HttpGet("template/xls")]
        [Authorize(Policy = "AdminOrSecretary")]
        public IActionResult DownloadStudentImportTemplate()
        {
            var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("ModeloAlunos");

            var headers = new[]
            {
                "NomeCompleto",
                "Sexo",
                "DataNascimento",
                "Endereco",
                "Municipio",
                "Estado",
                "UnidadeEscolar",
                "MunicipioEscola",
                "EstadoEscola",
                "Serie",
                "NomeResponsavel",
                "EstadoCivilPais",
                "CorPele",
                "SUS",
                "RG",
                "CPF"
            };

            var headerRow = sheet.CreateRow(0);
            var headerStyle = workbook.CreateCellStyle();
            var font = workbook.CreateFont();
            font.IsBold = true;
            headerStyle.SetFont(font);

            for (var i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }

            var exampleRow = sheet.CreateRow(1);
            exampleRow.CreateCell(0).SetCellValue("João Silva");
            exampleRow.CreateCell(1).SetCellValue("Masculino");
            exampleRow.CreateCell(2).SetCellValue("15/05/2010");
            exampleRow.CreateCell(3).SetCellValue("Rua Exemplo, 123");
            exampleRow.CreateCell(4).SetCellValue("São Paulo");
            exampleRow.CreateCell(5).SetCellValue("SP");
            exampleRow.CreateCell(6).SetCellValue("Escola Municipal");
            exampleRow.CreateCell(7).SetCellValue("São Paulo");
            exampleRow.CreateCell(8).SetCellValue("SP");
            exampleRow.CreateCell(9).SetCellValue("5º Ano");
            exampleRow.CreateCell(10).SetCellValue("Maria Silva");
            exampleRow.CreateCell(11).SetCellValue("Casados");
            exampleRow.CreateCell(12).SetCellValue("Parda");
            exampleRow.CreateCell(13).SetCellValue("");
            exampleRow.CreateCell(14).SetCellValue("");
            exampleRow.CreateCell(15).SetCellValue("");

            for (var i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            using var stream = new MemoryStream();
            workbook.Write(stream);
            return File(
                stream.ToArray(),
                "application/vnd.ms-excel",
                "modelo-cadastro-alunos.xls"
            );
        }

        [HttpPost("import/xls")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = "AdminOrSecretary")]
        public async Task<IActionResult> ImportStudentsFromSpreadsheet(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nenhuma planilha foi enviada."
                    });
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xls" && extension != ".xlsx")
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Formato inválido. Envie um arquivo .xls ou .xlsx."
                    });
                }

                using var stream = file.OpenReadStream();
                IWorkbook workbook = extension == ".xls"
                    ? new HSSFWorkbook(stream)
                    : new XSSFWorkbook(stream);

                var sheet = workbook.GetSheetAt(0);
                if (sheet == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Não foi possível ler a planilha enviada."
                    });
                }

                var createdCount = 0;
                var logs = new List<Log>();
                var successfulStudents = new List<StudentImportSuccessItem>();
                var failedStudents = new List<StudentImportFailureItem>();

                for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null || IsRowEmpty(row, 13)) // First 13 are required mostly
                    {
                        continue;
                    }

                    var rowData = BuildSpreadsheetRowData(row);
                    var rowErrors = ValidateSpreadsheetRow(rowData);

                    if (rowErrors.Count > 0)
                    {
                        failedStudents.Add(BuildFailureItem(rowIndex + 1, rowData, rowErrors));
                        continue;
                    }

                    var normalizedSchoolCity = Helpers.NormalizeCity(rowData.SchoolCity);
                    var fullSchoolState = GetFullStateName(rowData.SchoolState);
                    var normalizedSchoolState = Helpers.NormalizeState(fullSchoolState);
                    var normalizedSchoolName = rowData.SchoolName.Trim();

                    var school = await _appDbContext.Schools
                        .FirstOrDefaultAsync(s => s.City.ToLower() == normalizedSchoolCity.ToLower() &&
                                                  s.State.ToLower() == normalizedSchoolState.ToLower() &&
                                                  s.Name.ToLower() == normalizedSchoolName.ToLower());

                    if (school == null)
                    {
                        failedStudents.Add(BuildFailureItem(rowIndex + 1, rowData, $"Escola '{normalizedSchoolName}' na cidade '{normalizedSchoolCity}' não encontrada."));
                        continue;
                    }

                    TryParseSpreadsheetDate(rowData.DateOfBirth, out var dateOfBirth);

                    var studentRequest = new StudentRequest
                    {
                        FullName = rowData.FullName,
                        Sex = rowData.Sex,
                        DateOfBirth = dateOfBirth ?? DateTime.MinValue,
                        Address = rowData.Address,
                        City = Helpers.NormalizeCity(rowData.City),
                        State = Helpers.NormalizeState(GetFullStateName(rowData.State)),
                        SchoolName = school.Name,
                        SchoolCity = school.City,
                        SchoolState = school.State,
                        Serie = rowData.Serie,
                        NameOfPersonResponsible = rowData.NameOfPersonResponsible,
                        MaritalStatusParents = rowData.MaritalStatusParents,
                        SkinColor = rowData.SkinColor,
                        SUSCard = Regex.Replace(rowData.SUSCard ?? "", @"\D", ""),
                        RG = Regex.Replace(rowData.RG ?? "", @"[^\w\d]", ""),
                        CPF = Regex.Replace(rowData.CPF ?? "", @"\D", "")
                    };

                    if (!string.IsNullOrWhiteSpace(studentRequest.CPF))
                    {
                        var cpfExists = await _appDbContext.Students.AnyAsync(s => s.CPF == studentRequest.CPF);
                        if (cpfExists)
                        {
                            failedStudents.Add(BuildFailureItem(rowIndex + 1, rowData, $"Já existe um estudante cadastrado com este CPF ({studentRequest.CPF})."));
                            continue;
                        }
                    }

                    var student = StudentMapper.ToEntity(studentRequest, school);
                    await _appDbContext.Students.AddAsync(student);
                    
                    try 
                    {
                        await _appDbContext.SaveChangesAsync();
                    }
                    catch (DbUpdateException dbEx)
                    {
                        var message = dbEx.InnerException?.Message ?? dbEx.Message;
                        failedStudents.Add(BuildFailureItem(rowIndex + 1, rowData, $"Erro ao salvar no banco de dados: {message}"));
                        continue;
                    }

                    createdCount++;
                    successfulStudents.Add(new StudentImportSuccessItem
                    {
                        RowNumber = rowIndex + 1,
                        FullName = student.FullName,
                        SchoolName = school.Name
                    });

                    logs.Add(new Log
                    {
                        LogLevel = Models.Enums.LogLevel.Info.ToString(),
                        Message = $"Aluno criado em lote com ID {student.ID}",
                        Source = "StudentsController",
                        UserID = CurrentUserGuid,
                        EntityType = "Student",
                        EntityID = student.ID,
                        Details = "",
                        Action = "CreateBulk"
                    });
                }

                if (logs.Count > 0)
                {
                    await _appDbContext.Logs.AddRangeAsync(logs);
                    await _appDbContext.SaveChangesAsync();
                }

                if (createdCount > 0)
                {
                    await _notificationService.CreateNotificationAdmins(
                        type: TypeNotification.System,
                        title: "Cadastro em lote de alunos",
                        message: $"{createdCount} aluno(s) criado(s) por importação de planilha.",
                        actionURL: "api/v1/students"
                    );
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Importação concluída.",
                    Data = new
                    {
                        createdCount,
                        failedCount = failedStudents.Count,
                        successfulStudents,
                        failedStudents
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao importar alunos por planilha");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro na importação da planilha. {ex.Message}"
                });
            }
        }

        private static StudentImportSpreadsheetRow BuildSpreadsheetRowData(IRow row)
        {
            return new StudentImportSpreadsheetRow
            {
                FullName = GetCellString(row, 0),
                Sex = GetCellString(row, 1),
                DateOfBirth = GetCellString(row, 2),
                Address = GetCellString(row, 3),
                City = GetCellString(row, 4),
                State = GetCellString(row, 5),
                SchoolName = GetCellString(row, 6),
                SchoolCity = GetCellString(row, 7),
                SchoolState = GetCellString(row, 8),
                Serie = GetCellString(row, 9),
                NameOfPersonResponsible = GetCellString(row, 10),
                MaritalStatusParents = GetCellString(row, 11),
                SkinColor = GetCellString(row, 12),
                SUSCard = string.IsNullOrWhiteSpace(GetCellString(row, 13)) ? null : GetCellString(row, 13),
                RG = string.IsNullOrWhiteSpace(GetCellString(row, 14)) ? null : GetCellString(row, 14),
                CPF = string.IsNullOrWhiteSpace(GetCellString(row, 15)) ? null : GetCellString(row, 15)
            };
        }

        private static List<string> ValidateSpreadsheetRow(StudentImportSpreadsheetRow rowData)
        {
            var rowErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(rowData.FullName)) rowErrors.Add("o campo NomeCompleto é obrigatório.");
            if (string.IsNullOrWhiteSpace(rowData.Sex)) rowErrors.Add("o campo Sexo é obrigatório.");
            if (!TryParseSpreadsheetDate(rowData.DateOfBirth, out _)) rowErrors.Add("o campo DataNascimento é obrigatório e deve estar em formato válido.");
            if (string.IsNullOrWhiteSpace(rowData.Address)) rowErrors.Add("o campo Endereco é obrigatório.");
            if (string.IsNullOrWhiteSpace(rowData.City)) rowErrors.Add("o campo Municipio é obrigatório.");
            if (string.IsNullOrWhiteSpace(rowData.SchoolName)) rowErrors.Add("o campo UnidadeEscolar é obrigatório.");
            if (string.IsNullOrWhiteSpace(rowData.SchoolCity)) rowErrors.Add("o campo MunicipioEscola é obrigatório.");
            if (string.IsNullOrWhiteSpace(rowData.Serie)) rowErrors.Add("o campo Serie é obrigatório.");
            if (string.IsNullOrWhiteSpace(rowData.NameOfPersonResponsible)) rowErrors.Add("o campo NomeResponsavel é obrigatório.");
            if (string.IsNullOrWhiteSpace(rowData.MaritalStatusParents)) rowErrors.Add("o campo EstadoCivilPais é obrigatório.");
            if (string.IsNullOrWhiteSpace(rowData.SkinColor)) rowErrors.Add("o campo CorPele é obrigatório.");

            return rowErrors;
        }

        private static StudentImportFailureItem BuildFailureItem(int rowNumber, StudentImportSpreadsheetRow rowData, params string[] errors)
        {
            return BuildFailureItem(rowNumber, rowData, errors.AsEnumerable());
        }

        private static StudentImportFailureItem BuildFailureItem(int rowNumber, StudentImportSpreadsheetRow rowData, IEnumerable<string> errors)
        {
            return new StudentImportFailureItem
            {
                RowNumber = rowNumber,
                FullName = rowData.FullName,
                Errors = errors.ToList(),
                RowData = rowData
            };
        }

        private static bool IsRowEmpty(IRow row, int expectedCells)
        {
            for (var i = 0; i < expectedCells; i++)
            {
                if (!string.IsNullOrWhiteSpace(GetCellString(row, i)))
                    return false;
            }
            return true;
        }

        private static string GetCellString(IRow row, int index)
        {
            var cell = row.GetCell(index);
            if (cell == null) return string.Empty;

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? (cell.DateCellValue?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? string.Empty)
                    : cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
                CellType.Boolean => cell.BooleanCellValue ? "true" : "false",
                CellType.Formula => cell.ToString()?.Trim() ?? string.Empty,
                _ => cell.ToString()?.Trim() ?? string.Empty
            };
        }

        private static bool TryParseSpreadsheetDate(string? rawValue, out DateTime? parsedDate)
        {
            parsedDate = null;
            if (string.IsNullOrWhiteSpace(rawValue)) return false;

            var acceptedFormats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };
            if (DateTime.TryParseExact(rawValue, acceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                parsedDate = parsed.Date;
                return true;
            }

            if (DateTime.TryParse(rawValue, out parsed))
            {
                parsedDate = parsed.Date;
                return true;
            }

            return false;
        }

        private static string GetFullStateName(string state)
        {
            if (string.IsNullOrWhiteSpace(state)) return string.Empty;
            var s = state.Trim().ToUpperInvariant();
            return s switch
            {
                "AC" => "Acre",
                "AL" => "Alagoas",
                "AP" => "Amapá",
                "AM" => "Amazonas",
                "BA" => "Bahia",
                "CE" => "Ceará",
                "DF" => "Distrito Federal",
                "ES" => "Espírito Santo",
                "GO" => "Goiás",
                "MA" => "Maranhão",
                "MT" => "Mato Grosso",
                "MS" => "Mato Grosso do Sul",
                "MG" => "Minas Gerais",
                "PA" => "Pará",
                "PB" => "Paraíba",
                "PR" => "Paraná",
                "PE" => "Pernambuco",
                "PI" => "Piauí",
                "RJ" => "Rio de Janeiro",
                "RN" => "Rio Grande do Norte",
                "RS" => "Rio Grande do Sul",
                "RO" => "Rondônia",
                "RR" => "Roraima",
                "SC" => "Santa Catarina",
                "SP" => "São Paulo",
                "SE" => "Sergipe",
                "TO" => "Tocantins",
                _ => state
            };
        }
    }
}
