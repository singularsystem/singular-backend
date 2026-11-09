using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Mappers;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SingularSystem.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<UsersController> _logger;
        private readonly NotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly IAuthorizationService _authorizationService;
        private const string DefaultNewUserPassword = "user123!";

        private sealed class UserImportSpreadsheetRow
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string Sex { get; set; } = string.Empty;
            public string DateOfBirth { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string? SchoolID { get; set; }
        }

        private sealed class UserImportSuccessItem
        {
            public int RowNumber { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        private sealed class UserImportFailureItem
        {
            public int RowNumber { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public List<string> Errors { get; set; } = new();
            public UserImportSpreadsheetRow RowData { get; set; } = new();
        }

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager, ILogger<UsersController> logger, AppDbContext appDbContext, NotificationService notificationService, IConfiguration configuration, IAuthorizationService authorizationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _appDbContext = appDbContext;
            _configuration = configuration;
            _logger = logger;
            _notificationService = notificationService;
            _authorizationService = authorizationService;
        }

        [HttpGet("template/xls")]
        [Authorize(Policy = "AdminOrMunicipalManager")]
        public IActionResult DownloadUserImportTemplate()
        {
            var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("ModeloUsuarios");

            var headers = new[]
            {
                "NomeCompleto",
                "Email",
                "Telefone",
                "Sexo (Feminino/Masculino/Outro)",
                "DataNascimento (dd/MM/yyyy)",
                "Perfil (Admin,Secretary,SocialWork,Psychologist,Psychopedagogue,Teacher,Coordinator,Principal,MunicipalManager)",
                "Estado",
                "Municipio",
                "Endereco",
                "Escola (Opcional)"
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
            exampleRow.CreateCell(0).SetCellValue("Joao da Silva");
            exampleRow.CreateCell(1).SetCellValue("joao.silva@email.com");
            exampleRow.CreateCell(2).SetCellValue("(11)99999-9999");
            exampleRow.CreateCell(3).SetCellValue("Masculino");
            exampleRow.CreateCell(4).SetCellValue("01/01/1990");
            exampleRow.CreateCell(5).SetCellValue("Teacher");
            exampleRow.CreateCell(6).SetCellValue("São Paulo");
            exampleRow.CreateCell(7).SetCellValue("Campinas");
            exampleRow.CreateCell(8).SetCellValue("Rua Exemplo, 100");
            exampleRow.CreateCell(9).SetCellValue("");

            for (var i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            using var stream = new MemoryStream();
            workbook.Write(stream);
            return File(
                stream.ToArray(),
                "application/vnd.ms-excel",
                "modelo-cadastro-usuarios.xls"
            );
        }

        [HttpPost("import/xls")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = "AdminOrMunicipalManager")]
        public async Task<IActionResult> ImportUsersFromSpreadsheet(IFormFile file)
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
                var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var logs = new List<Log>();
                var successfulUsers = new List<UserImportSuccessItem>();
                var failedUsers = new List<UserImportFailureItem>();

                for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null || IsRowEmpty(row, 10))
                    {
                        continue;
                    }

                    var rowData = BuildSpreadsheetRowData(row);
                    var rowErrors = ValidateSpreadsheetRow(rowData);

                    if (rowErrors.Count > 0)
                    {
                        failedUsers.Add(BuildFailureItem(rowIndex + 1, rowData, rowErrors));
                        continue;
                    }

                    var userRequest = BuildUserRequestFromSpreadsheetRow(rowData);

                    var roleExists = await _roleManager.RoleExistsAsync(userRequest.Type);
                    if (!roleExists)
                    {
                        failedUsers.Add(BuildFailureItem(
                            rowIndex + 1,
                            rowData,
                            $"perfil '{userRequest.Type}' não existe."
                        ));
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(rowData.SchoolID) && !userRequest.Type.Equals("Coordinator", StringComparison.OrdinalIgnoreCase))
                    {
                        failedUsers.Add(BuildFailureItem(
                            rowIndex + 1,
                            rowData,
                            $"O perfil '{userRequest.Type}' não permite o cadastro de unidade escolar. Apenas coordenadores ('Coordinator') podem ter escola vinculada."
                        ));
                        continue;
                    }

                    var existingByEmail = await _userManager.FindByEmailAsync(userRequest.Email);
                    if (existingByEmail != null)
                    {
                        failedUsers.Add(BuildFailureItem(
                            rowIndex + 1,
                            rowData,
                            $"email '{userRequest.Email}' já cadastrado."
                        ));
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(rowData.SchoolID))
                    {
                        School? school = null;
                        if (Guid.TryParse(rowData.SchoolID, out var schoolGuid))
                        {
                            school = await _appDbContext.Schools.FirstOrDefaultAsync(s => s.ID == schoolGuid);
                        }
                        else
                        {
                            var normalizedUserCity = Helpers.NormalizeCity(rowData.City);
                            var normalizedUserState = Helpers.NormalizeState(rowData.State);
                            
                            // Tenta buscar pelo nome da escola na mesma cidade/estado do usuário
                            school = await _appDbContext.Schools
                                .FirstOrDefaultAsync(s => s.City.ToLower() == normalizedUserCity.ToLower() &&
                                                          s.State.ToLower() == normalizedUserState.ToLower() &&
                                                          s.Name.ToLower() == rowData.SchoolID.Trim().ToLower());
                            
                            // Se não achar, tenta buscar apenas pelo nome da escola
                            school ??= await _appDbContext.Schools
                                .FirstOrDefaultAsync(s => s.Name.ToLower() == rowData.SchoolID.Trim().ToLower());
                        }

                        if (school == null)
                        {
                            failedUsers.Add(BuildFailureItem(
                                rowIndex + 1,
                                rowData,
                                $"Escola '{rowData.SchoolID}' não foi encontrada no sistema."
                            ));
                            continue;
                        }

                        userRequest.SchoolID = school.ID.ToString();
                    }

                    var names = userRequest.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (names.Length == 0)
                    {
                        failedUsers.Add(BuildFailureItem(rowIndex + 1, rowData, "nome completo inválido."));
                        continue;
                    }

                    var firstName = names[0];
                    var lastName = names.Length > 1 ? names[^1] : names[0];
                    var userName = $"{firstName}{lastName}_{Guid.NewGuid().ToString()[..6]}";
                    var userEntity = ApplicationUserMapper.ToEntity(userRequest, userName);
                    userEntity.MustChangePassword = true;
                    var createResult = await _userManager.CreateAsync(userEntity, DefaultNewUserPassword);

                    if (!createResult.Succeeded)
                    {
                        var translatedErrors = TranslateIdentityErrors(createResult.Errors);
                        failedUsers.Add(BuildFailureItem(rowIndex + 1, rowData, translatedErrors));
                        continue;
                    }

                    var addRoleResult = await _userManager.AddToRoleAsync(userEntity, userRequest.Type);
                    if (!addRoleResult.Succeeded)
                    {
                        await _userManager.DeleteAsync(userEntity);
                        var translatedRoleErrors = TranslateIdentityErrors(addRoleResult.Errors);
                        failedUsers.Add(BuildFailureItem(rowIndex + 1, rowData, translatedRoleErrors));
                        continue;
                    }

                    createdCount++;
                    successfulUsers.Add(new UserImportSuccessItem
                    {
                        RowNumber = rowIndex + 1,
                        FullName = userRequest.FullName,
                        Email = userRequest.Email
                    });

                    logs.Add(new Log
                    {
                        LogLevel = Models.Enums.LogLevel.Info.ToString(),
                        Message = $"Usuário criado em lote com ID {userEntity.Id}",
                        Source = "UsersController",
                        UserID = Guid.TryParse(actorUserId, out var actorGuid)
                            ? actorGuid
                            : Guid.Parse(userEntity.Id),
                        EntityType = "ApplicationUser",
                        EntityID = Guid.Parse(userEntity.Id),
                        Details = "",
                        Action = "RegisterBulk"
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
                        title: "Cadastro em lote de usuários",
                        message: $"{createdCount} usuário(s) criado(s) por importação de planilha.",
                        actionURL: "api/v1/users/import/xls"
                    );
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Importação concluída.",
                    Data = new
                    {
                        createdCount,
                        failedCount = failedUsers.Count,
                        defaultPassword = DefaultNewUserPassword,
                        successfulUsers,
                        failedUsers
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao importar usuários por planilha");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro na importação da planilha. {ex.Message}"
                });
            }
        }

        [HttpGet("email-availability")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckEmailAvailability([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Email é obrigatório."
                    });
                }

                var existingUser = await _userManager.FindByEmailAsync(email);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Disponibilidade de email verificada com sucesso.",
                    Data = new
                    {
                        available = existingUser == null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar disponibilidade de email");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao verificar disponibilidade de email."
                });
            }
        }

        [HttpPost("register")]
        [Authorize(Policy = "AdminOrMunicipalManager")]
        public async Task<IActionResult> Register([FromBody] UserRequest model)
        {
            try
            {
                model.Password = DefaultNewUserPassword;
                model.ConfirmPassword = DefaultNewUserPassword;

                var roleExists = await _roleManager.RoleExistsAsync(model.Type);

                if (!roleExists)
                {
                    return BadRequest(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Perfil '{model.Type}' não existe!"
                    });
                }

                if (!string.IsNullOrWhiteSpace(model.SchoolID) && !model.Type.Equals("Coordinator", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"O perfil '{model.Type}' não permite o cadastro de unidade escolar. Apenas coordenadores ('Coordinator') podem ter escola vinculada."
                    });
                }

                string[] names = model.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                string userName = $"{names[0]}{names[^1]}_{Guid.NewGuid().ToString()[..6]}";

                ApplicationUser user = ApplicationUserMapper.ToEntity(model, userName);
                user.MustChangePassword = true;

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Type);

                    await _appDbContext.Logs.AddAsync(new Log
                    {
                        LogLevel = Models.Enums.LogLevel.Info.ToString(),
                        Message = $"Usuário criado com sucesso com ID {user.Id}",
                        Source = "UsersController",
                        UserID = Guid.Parse(user.Id),
                        EntityType = "ApplicationUser",
                        EntityID = Guid.Parse(user.Id),
                        Details = "",
                        Action = "Register"
                    });
                    await _appDbContext.SaveChangesAsync();

                    await _notificationService.CreateNotificationAdmins(
                        type: TypeNotification.System,
                        title: "Novo usuário criado!",
                        message: "Um novo usuário foi criado.",
                        actionURL: "api/v1/users/register"
                    );

                    return StatusCode(201, new ApiResponse<object>()
                    {
                        Success = true,
                        Message = "Usuário criado com sucesso"
                    });
                }
                else
                {
                    var errorMessages = result.Errors.Select(error =>
                    {
                        var errorTranslations = new Dictionary<string, string>
                        {
                            { "DuplicateUserName", "Este nome de usuário já está em uso." },
                            { "DuplicateEmail", "Este email já está cadastrado." },
                            { "InvalidEmail", "Formato de email inválido." },
                            { "PasswordTooShort", "A senha deve ter no mínimo {0} caracteres." },
                            { "PasswordRequiresNonAlphanumeric", "A senha deve conter pelo menos um caractere especial." },
                            { "PasswordRequiresDigit", "A senha deve conter pelo menos um número (0-9)." },
                            { "PasswordRequiresUpper", "A senha deve conter pelo menos uma letra maiúscula." },
                            { "PasswordRequiresLower", "A senha deve conter pelo menos uma letra minúscula." },
                            { "PasswordMismatch", "As senhas não coincidem." }
                        };

                        if (errorTranslations.ContainsKey(error.Code))
                        {
                            return string.Format(errorTranslations[error.Code], error.Description);
                        }

                        return error.Description;
                    }).ToList();

                    _logger.LogWarning("Falha ao criar usuário: {Errors}", string.Join(", ", errorMessages));

                    return BadRequest(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Erro ao criar usuário",
                        Data = errorMessages
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro na cadastrar novo usuário. {ex.Message}"
                });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null || !user.IsActive)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Usuário desativado ou não encontrado"
                    });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(
                    user,
                    model.Password,
                    lockoutOnFailure: true
                );

                if (result.Succeeded)
                {
                    var mustChangePassword = user.MustChangePassword;
                    var roles = await _userManager.GetRolesAsync(user);
                    var jwtSecret = _configuration["JWTSecret"];
                    var signInKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret!)
                    );

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id)
                    };

                    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

                    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims);

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = claimsIdentity,
                        Expires = DateTime.UtcNow.AddMinutes(120),
                        SigningCredentials = new SigningCredentials(
                            signInKey,
                            SecurityAlgorithms.HmacSha256
                        ),
                        Issuer = _configuration["JwtIssuer"],
                        Audience = _configuration["JwtAudience"],
                    };

                    var tokenHandler = new JwtSecurityTokenHandler();
                    var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                    var token = tokenHandler.WriteToken(securityToken);

                    await _appDbContext.Logs.AddAsync(new Log
                    {
                        LogLevel = Models.Enums.LogLevel.Info.ToString(),
                        Message = $"Usuário logado com sucesso com ID {user.Id}",
                        Source = "UsersController",
                        UserID = Guid.Parse(user.Id),
                        EntityType = "ApplicationUser",
                        EntityID = Guid.Parse(user.Id),
                        Details = "",
                        Action = "Login"
                    });
                    await _appDbContext.SaveChangesAsync();

                    user.LastLogin = DateTime.UtcNow;
                    await _userManager.ResetAccessFailedCountAsync(user);
                    await _userManager.UpdateAsync(user);

                    return Ok(new ApiResponse<LoginResponse>()
                    {
                        Success = true,
                        Message = "Login realizado com sucesso",
                        Data = new LoginResponse()
                        {
                            ID = user.Id,
                            Token = token,
                            TokenExpiration = tokenDescriptor.Expires,
                            MustChangePassword = mustChangePassword
                        }
                    });
                }
                else if (result.IsLockedOut)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Esta conta está temporariamente bloqueada devido a excesso de tentativas de login incorretas. Tente novamente em alguns minutos."
                    });
                }
                else if (result.IsNotAllowed)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Acesso não permitido. Por favor, verifique se sua conta foi ativada."
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Usuário ou senha incorretos",
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro no login do usuário. {ex.Message}"
                });
            }
        }

        //[Authorize(Roles = "Admin,MunicipalManager")]
        [Authorize(Policy = "AdminOrMunicipalManager")]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string searchTerm = "", [FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string role = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Min(pageSize, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = currentUserId != null ? await _userManager.FindByIdAsync(currentUserId) : null;
                
                if (User.IsInRole("MunicipalManager") && currentUser != null)
                {
                    city = currentUser.City;
                }

                var query = _userManager.Users.Include(s => s.School)
                                              .AsNoTracking();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(u => EF.Functions.Like(u.FullName, $"%{searchTerm}%") || EF.Functions.Like(u.Email, $"%{searchTerm}%"));
                }

                if (!string.IsNullOrWhiteSpace(state))
                {
                    query = query.Where(u => u.State == state);
                }

                if (!string.IsNullOrWhiteSpace(city))
                {
                    query = query.Where(u => u.City == city);
                }

                if (!string.IsNullOrWhiteSpace(role) && role.ToLower() != "todos")
                {
                    var roleEntity = await _roleManager.FindByNameAsync(role);
                    if (roleEntity != null)
                    {
                        var usersInRole = await _appDbContext.UserRoles
                            .Where(ur => ur.RoleId == roleEntity.Id)
                            .Select(ur => ur.UserId)
                            .ToListAsync();
                            
                        query = query.Where(u => usersInRole.Contains(u.Id));
                    }
                }

                var total = await query.CountAsync();

                var users = await query.OrderByDescending(u => u.IsActive)
                                       .ThenBy(u => u.CreatedAt)
                                       .Skip(skip)
                                       .Take(pageSize)
                                       .ToListAsync();

                if (users == null || users.Count == 0)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Nenhum usuário encontrado"
                    });
                }

                var userIds = users.Select(u => u.Id).ToList();

                var rolesByUser = await _appDbContext.UserRoles.Where(ur => userIds.Contains(ur.UserId))
                                                               .Join(_appDbContext.Roles,
                                                                     ur => ur.RoleId,
                                                                     r => r.Id,
                                                                     (ur, r) => new { ur.UserId, r.Name })
                                                               .GroupBy(x => x.UserId)
                                                               .ToDictionaryAsync(
                                                                   g => g.Key,
                                                                   g => g.Select(x => x.Name).ToList()
                                                               );

                if (rolesByUser == null)
                {
                    return BadRequest();
                }

                var response = new List<UserResponse>();

                foreach (var user in users)
                {
                    var roles = rolesByUser!.GetValueOrDefault(user.Id, new List<string>());

                    response.Add(ApplicationUserMapper.ToResponse(user, roles));
                }

                return Ok(new ApiResponse<List<UserResponse>>
                {
                    Success = true,
                    Message = "Lista de usuários retornada com sucesso",
                    Data = response,
                    Total = total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro no retorno da lista de usuários. {ex.Message}"
                });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userManager.Users.Include(u => u.School).FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {

                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Usuário com ID {userId} não foi encontrado"
                    });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var userResponse = ApplicationUserMapper.ToResponse(user, roles);

                return Ok(new ApiResponse<UserResponse>
                {
                    Success = true,
                    Message = "Usuário carregado com sucesso",
                    Data = userResponse
                });

            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro no retorno do usuário. {ex.Message}"
                });
            }
        }

        //[Authorize(Roles = "Admin,Secretary")]
        [Authorize(Policy = "AdminOrSecretary")]
        [HttpGet("principals")]
        public async Task<IActionResult> GetAllPrincipals()
        {
            try
            {
                var principals = await _appDbContext.Users.Where(u => u.IsActive)
                                                          .Join(_appDbContext.UserRoles,
                                                                u => u.Id,
                                                                ur => ur.UserId,
                                                                (u, ur) => new { u, ur })
                                                          .Join(_appDbContext.Roles,
                                                                x => x.ur.RoleId,
                                                                r => r.Id,
                                                                (x, r) => new { x.u, r.Name })
                                                          .Where(x => x.Name == "Principal")
                                                          .Select(x => x.u)
                                                          .Include(u => u.School)
                                                          .AsNoTracking()
                                                          .ToListAsync();

                if (principals == null || principals.Count == 0)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Nenhum usuário encontrado"
                    });
                }

                var response = new List<UserResponse>();
                var roles = new List<string> { "Principal" };

                foreach (var principal in principals)
                {
                    response.Add(ApplicationUserMapper.ToResponse(principal, roles));
                }

                if (response == null || response.Count == 0)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nenhum usuário encontrado"
                    });
                }

                return Ok(new ApiResponse<List<UserResponse>>
                {
                    Success = true,
                    Message = "Lista de usuários retornada com sucesso",
                    Data = response,
                    Total = response.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro no retorno da lista de diretores. {ex.Message}"
                });
            }
        }

        //[Authorize(Roles = "Admin,Secretary")]
        [Authorize(Policy = "AdminOrSecretary")]
        [HttpGet("principals/{city}")]
        public async Task<IActionResult> GetAllPrincipalsByCity(string city)
        {
            try
            {
                var principals = await _appDbContext.Users.Where(u => u.IsActive)
                                                    .Join(_appDbContext.UserRoles,
                                                        u => u.Id,
                                                        ur => ur.UserId,
                                                        (u, ur) => new { u, ur })
                                                    .Join(_appDbContext.Roles,
                                                        x => x.ur.RoleId,
                                                        r => r.Id,
                                                        (x, r) => new { x.u, r.Name })
                                                    .Where(x => x.Name == "Principal")
                                                    .Select(x => x.u)
                                                    .Include(u => u.School)
                                                    .Where(u => u.City == city)
                                                    .AsNoTracking()
                                                    .ToListAsync();

                if (principals == null || principals.Count == 0)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = $"Nenhum usuário encontrado na cidade {city}"
                    });
                }

                var response = new List<UserResponse>();
                var roles = new List<string> { "Principal" };

                foreach (var user in principals)
                {
                    response.Add(ApplicationUserMapper.ToResponse(user, roles));
                }

                if (response == null || response.Count == 0)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nenhum usuário encontrado"
                    });
                }

                return Ok(new ApiResponse<List<UserResponse>>
                {
                    Success = true,
                    Message = "Lista de usuários retornada com sucesso",
                    Data = response,
                    Total = response.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro no retorno dos diretores por cidade. {ex.Message}"
                });
            }
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UserUpdateRequest model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var authResult = await _authorizationService.AuthorizeAsync(
                                                                User,
                                                                model.ID.ToString(),
                                                                "SelfOrAdminOrMunicipalManager"
                                                            );

                if (!authResult.Succeeded)
                    return Forbid();

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Erros de validação",
                        Data = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var userToUpdate = await _userManager.FindByIdAsync(model.ID.ToString());

                if (userToUpdate == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Usuário com ID {model.ID} não foi encontrado"
                    });
                }

                var userRoles = await _userManager.GetRolesAsync(userToUpdate);
                var isCoordinator = userRoles.Any(r => r.Equals("Coordinator", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(model.SchoolID) && !isCoordinator)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Este perfil de usuário não permite o cadastro de unidade escolar. Apenas coordenadores ('Coordinator') podem ter escola vinculada."
                    });
                }

                Guid? modelSchoolId = string.IsNullOrWhiteSpace(model.SchoolID) ? null : Guid.Parse(model.SchoolID);

                if (
                    userToUpdate.Email == model.Email &&
                    userToUpdate.FullName == model.FullName &&
                    userToUpdate.Address == model.Address &&
                    userToUpdate.City == model.City &&
                    userToUpdate.State == model.State &&
                    userToUpdate.PhoneNumber == model.PhoneNumber &&
                    userToUpdate.SchoolID == modelSchoolId
                )
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Nenhuma alteração detectada nos dados"
                    });
                }

                ApplicationUserMapper.UpdateEntity(userToUpdate, model);

                var result = await _userManager.UpdateAsync(userToUpdate);

                if (!result.Succeeded)
                {
                    var errorMessages = result.Errors.Select(error =>
                    {
                        var errorTranslations = new Dictionary<string, string>
                        {
                            { "DuplicateUserName", "Este nome de usuário já está em uso." },
                            { "DuplicateEmail", "Este email já está cadastrado." },
                            { "InvalidEmail", "Formato de email inválido." },
                            { "PasswordTooShort", "A senha deve ter no mínimo {0} caracteres." },
                            { "PasswordRequiresNonAlphanumeric", "A senha deve conter pelo menos um caractere especial." },
                            { "PasswordRequiresDigit", "A senha deve conter pelo menos um número (0-9)." },
                            { "PasswordRequiresUpper", "A senha deve conter pelo menos uma letra maiúscula." },
                            { "PasswordRequiresLower", "A senha deve conter pelo menos uma letra minúscula." },
                            { "PasswordMismatch", "As senhas não coincidem." }
                        };

                        if (errorTranslations.ContainsKey(error.Code))
                        {
                            return string.Format(errorTranslations[error.Code], error.Description);
                        }

                        return error.Description;
                    }).ToList();

                    _logger.LogWarning("Falha ao atualizar usuário: {Errors}", string.Join(", ", errorMessages));

                    return BadRequest(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Erro ao atualizar usuário",
                        Data = errorMessages
                    });
                }

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Usuário atualizado com sucesso com ID {userToUpdate.Id}",
                    Source = "UsersController",
                    UserID = Guid.Parse(userId),
                    EntityType = "ApplicationUser",
                    EntityID = Guid.Parse(userToUpdate.Id),
                    Details = "",
                    Action = "Update"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Usuário atualizado!",
                    message: $"O usuário {model.ID} foi atualizado.",
                    actionURL: "api/v1/users"
                );

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Usuário atualizado com sucesso",
                    Data = new
                    {
                        userToUpdate.Id,
                        userToUpdate.Email,
                        userToUpdate.FullName,
                        userToUpdate.Address,
                        userToUpdate.City
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar dados do usuário");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao atualizar o usuário"
                });
            }
        }

        [Authorize]
        [HttpPut("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var authResult = await _authorizationService.AuthorizeAsync(
                                                            User,
                                                            model.ID.ToString(),
                                                            "SelfOrAdminOrMunicipalManager"
                                                        );

            if (!authResult.Succeeded)
                return Forbid();

            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            var userToUpdate = await _userManager.FindByIdAsync(model.ID.ToString());

            if (userToUpdate == null)
            {
                return NotFound(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Nenhum usuário foi encontrado com o ID {model.ID}"
                });
            }

            if (userId == model.ID.ToString())
            {
                IdentityResult updatePassword;
                var wasMustChange = userToUpdate.MustChangePassword;
                userToUpdate.MustChangePassword = false;

                if (wasMustChange)
                {
                    foreach (var validator in _userManager.PasswordValidators)
                    {
                        var validationResult = await validator.ValidateAsync(_userManager, userToUpdate, model.NewPassword);
                        if (!validationResult.Succeeded)
                        {
                            return BadRequest(new ApiResponse<object>
                            {
                                Success = false,
                                Message = string.Join("; ", TranslateIdentityErrors(validationResult.Errors))
                            });
                        }
                    }

                    userToUpdate.PasswordHash = _userManager.PasswordHasher.HashPassword(userToUpdate, model.NewPassword);
                    updatePassword = await _userManager.UpdateSecurityStampAsync(userToUpdate);
                }
                else
                {
                    updatePassword = await _userManager.ChangePasswordAsync(userToUpdate, model.CurrentPassword ?? string.Empty, model.NewPassword);
                }

                if (!updatePassword.Succeeded)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = updatePassword.Errors != null
                                    ? string.Join("; ", TranslateIdentityErrors(updatePassword.Errors))
                                    : "Erro desconhecido ao alterar a senha."
                    });
                }

                else
                {
                    userToUpdate.LockoutEnd = null;
                    await _userManager.ResetAccessFailedCountAsync(userToUpdate);
                    await _userManager.UpdateAsync(userToUpdate);

                    await _appDbContext.Logs.AddAsync(new Log
                    {
                        LogLevel = Models.Enums.LogLevel.Info.ToString(),
                        Message = $"Senha do usuário com ID {model.ID} atualizada com sucesso",
                        Source = "UsersController",
                        UserID = Guid.Parse(userId),
                        EntityType = "ApplicationUser",
                        EntityID = Guid.Parse(model.ID),
                        Details = "",
                        Action = "Update"
                    });
                    await _appDbContext.SaveChangesAsync();

                    await _notificationService.CreateNotificationAdmins(
                        type: TypeNotification.System,
                        title: "Senha atualizada!",
                        message: $"A senha de acesso do usuário {model.ID} foi atualizada.",
                        actionURL: "api/v1/users"
                    );

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Senha atualizada com sucesso",
                    });
                }
            }
            else
            {
                userToUpdate.MustChangePassword = true;

                foreach (var validator in _userManager.PasswordValidators)
                {
                    var validationResult = await validator.ValidateAsync(_userManager, userToUpdate, model.NewPassword);
                    if (!validationResult.Succeeded)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = string.Join("; ", TranslateIdentityErrors(validationResult.Errors))
                        });
                    }
                }

                userToUpdate.PasswordHash = _userManager.PasswordHasher.HashPassword(userToUpdate, model.NewPassword);
                var resetPasswordResult = await _userManager.UpdateSecurityStampAsync(userToUpdate);

                if (!resetPasswordResult.Succeeded)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = resetPasswordResult.Errors != null
                                    ? string.Join("; ", TranslateIdentityErrors(resetPasswordResult.Errors))
                                    : "Erro desconhecido ao alterar a senha."
                    });
                }
                else
                {
                    userToUpdate.LockoutEnd = null;
                    await _userManager.ResetAccessFailedCountAsync(userToUpdate);
                    await _userManager.UpdateAsync(userToUpdate);

                    await _appDbContext.Logs.AddAsync(new Log
                    {
                        LogLevel = Models.Enums.LogLevel.Info.ToString(),
                        Message = $"Senha do usuário com ID {model.ID} atualizada com sucesso",
                        Source = "UsersController",
                        UserID = Guid.Parse(userId),
                        EntityType = "ApplicationUser",
                        EntityID = Guid.Parse(model.ID),
                        Details = "",
                        Action = "Update"
                    });
                    await _appDbContext.SaveChangesAsync();

                    await _notificationService.CreateNotificationAdmins(
                        type: TypeNotification.System,
                        title: "Senha atualizada!",
                        message: $"A senha de acesso do usuário {model.ID} foi atualizada.",
                        actionURL: "api/v1/users"
                    );

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Senha atualizada com sucesso",
                    });
                }
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var authResult = await _authorizationService.AuthorizeAsync(
                                                                User,
                                                                id.ToString(),
                                                                "SelfOrAdmin"
                                                            );

                if (!authResult.Succeeded)
                    return Forbid();

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Erros de validação",
                        Data = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var userToDeactivate = await _userManager.FindByIdAsync(id.ToString());

                if (userToDeactivate == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Usuário com ID {id} não foi encontrado"
                    });
                }

                userToDeactivate.IsActive = false;
                userToDeactivate.UpdatedAt = DateTime.UtcNow;

                await _userManager.UpdateAsync(userToDeactivate);
                await _userManager.UpdateSecurityStampAsync(userToDeactivate);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Usuário desativado com sucesso com ID {userToDeactivate.Id}",
                    Source = "UsersController",
                    UserID = Guid.Parse(userId),
                    EntityType = "ApplicationUser",
                    EntityID = Guid.Parse(userToDeactivate.Id),
                    Details = "",
                    Action = "Deactivate"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Usuário desativado!",
                    message: $"O usuário {id} foi desativado.",
                    actionURL: "api/v1/users"
                );

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Usuário desativado com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar usuário");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao desativar usuário"
                });
            }
        }

        //[Authorize(Roles = "Admin")]
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id}/reactivate")]
        public async Task<IActionResult> ReactivateUser(Guid id)
        {
            var user = await _appDbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id.ToString());

            if (user == null)
                return NotFound();

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Usuário reativado com sucesso"
            });
        }

        [Authorize(Policy = "AdminOrMunicipalManager")]
        [HttpPost("bulk-reset-password")]
        public async Task<IActionResult> BulkResetPassword([FromBody] List<string> emails)
        {
            if (emails == null || emails.Count == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Nenhum email fornecido."
                });
            }

            var succeeded = new List<string>();
            var failed = new List<object>();

            foreach (var email in emails)
            {
                var user = await _userManager.FindByEmailAsync(email.Trim());

                if (user == null)
                {
                    failed.Add(new { email, reason = "Usuário não encontrado." });
                    continue;
                }

                user.MustChangePassword = true;

                bool validationFailed = false;
                var validationErrors = new List<string>();
                foreach (var validator in _userManager.PasswordValidators)
                {
                    var validationResult = await validator.ValidateAsync(_userManager, user, DefaultNewUserPassword);
                    if (!validationResult.Succeeded)
                    {
                        validationFailed = true;
                        validationErrors.AddRange(validationResult.Errors.Select(e => e.Description));
                    }
                }

                IdentityResult result;
                if (validationFailed)
                {
                    result = IdentityResult.Failed(validationErrors.Select(desc => new IdentityError { Description = desc }).ToArray());
                }
                else
                {
                    user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, DefaultNewUserPassword);
                    result = await _userManager.UpdateSecurityStampAsync(user);
                }

                if (!result.Succeeded)
                {
                    failed.Add(new { email, reason = string.Join("; ", result.Errors.Select(e => e.Description)) });
                    continue;
                }

                succeeded.Add(email);
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = $"Reset concluído: {succeeded.Count} sucesso(s), {failed.Count} falha(s).",
                Data = new { succeeded, failed }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Logout realizado com sucesso"
            });
        }

        private static UserImportSpreadsheetRow BuildSpreadsheetRowData(IRow row)
        {
            return new UserImportSpreadsheetRow
            {
                FullName = GetCellString(row, 0),
                Email = GetCellString(row, 1),
                PhoneNumber = GetCellString(row, 2),
                Sex = GetCellString(row, 3),
                DateOfBirth = GetCellString(row, 4),
                Type = GetCellString(row, 5),
                State = GetCellString(row, 6),
                City = GetCellString(row, 7),
                Address = GetCellString(row, 8),
                SchoolID = string.IsNullOrWhiteSpace(GetCellString(row, 9))
                    ? null
                    : GetCellString(row, 9)
            };
        }

        private static List<string> ValidateSpreadsheetRow(UserImportSpreadsheetRow rowData)
        {
            var rowErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(rowData.FullName))
                rowErrors.Add("o campo NomeCompleto é obrigatório.");

            if (string.IsNullOrWhiteSpace(rowData.Email))
                rowErrors.Add("o campo Email é obrigatório.");

            if (string.IsNullOrWhiteSpace(rowData.Sex))
                rowErrors.Add("o campo Sexo é obrigatório.");

            if (!TryParseSpreadsheetDate(rowData.DateOfBirth, out _))
                rowErrors.Add("o campo DataNascimento é obrigatório e deve estar em formato válido.");

            if (string.IsNullOrWhiteSpace(rowData.Type))
                rowErrors.Add("o campo Perfil é obrigatório.");

            if (string.IsNullOrWhiteSpace(rowData.City))
                rowErrors.Add("o campo Municipio é obrigatório.");

            return rowErrors;
        }

        private static UserRequest BuildUserRequestFromSpreadsheetRow(UserImportSpreadsheetRow rowData)
        {
            TryParseSpreadsheetDate(rowData.DateOfBirth, out var dateOfBirth);

            return new UserRequest
            {
                FullName = rowData.FullName,
                Email = rowData.Email,
                PhoneNumber = rowData.PhoneNumber,
                Sex = rowData.Sex,
                DateOfBirth = dateOfBirth ?? DateTime.MinValue,
                Type = rowData.Type,
                Password = DefaultNewUserPassword,
                ConfirmPassword = DefaultNewUserPassword,
                State = Helpers.NormalizeState(rowData.State),
                City = Helpers.NormalizeCity(rowData.City),
                Address = rowData.Address,
                SchoolID = rowData.SchoolID
            };
        }

        private static UserImportFailureItem BuildFailureItem(int rowNumber, UserImportSpreadsheetRow rowData, params string[] errors)
        {
            return BuildFailureItem(rowNumber, rowData, errors.AsEnumerable());
        }

        private static UserImportFailureItem BuildFailureItem(int rowNumber, UserImportSpreadsheetRow rowData, IEnumerable<string> errors)
        {
            return new UserImportFailureItem
            {
                RowNumber = rowNumber,
                FullName = rowData.FullName,
                Email = rowData.Email,
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
            if (cell == null)
                return string.Empty;

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
            if (string.IsNullOrWhiteSpace(rawValue))
                return false;

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

        private static List<string> TranslateIdentityErrors(IEnumerable<IdentityError> errors)
        {
            var errorTranslations = new Dictionary<string, string>
            {
                { "DuplicateUserName", "Este nome de usuário já está em uso." },
                { "DuplicateEmail", "Este email já está cadastrado." },
                { "InvalidEmail", "Formato de email inválido." },
                { "PasswordTooShort", "A senha deve ter no mínimo 6 caracteres." },
                { "PasswordRequiresNonAlphanumeric", "A senha deve conter pelo menos um caractere especial (!@#$...)." },
                { "PasswordRequiresDigit", "A senha deve conter pelo menos um número (0-9)." },
                { "PasswordRequiresUpper", "A senha deve conter pelo menos uma letra maiúscula." },
                { "PasswordRequiresLower", "A senha deve conter pelo menos uma letra minúscula ('a'-'z')." },
                { "PasswordMismatch", "As senhas não coincidem." }
            };

            return errors.Select(error =>
            {
                if (error.Code != null && errorTranslations.TryGetValue(error.Code, out var translation))
                {
                    return translation;
                }

                if (!string.IsNullOrWhiteSpace(error.Description))
                {
                    if (error.Description.Contains("lowercase", StringComparison.OrdinalIgnoreCase))
                        return "A senha deve conter pelo menos uma letra minúscula ('a'-'z').";
                    if (error.Description.Contains("uppercase", StringComparison.OrdinalIgnoreCase))
                        return "A senha deve conter pelo menos uma letra maiúscula ('A'-'Z').";
                    if (error.Description.Contains("digit", StringComparison.OrdinalIgnoreCase))
                        return "A senha deve conter pelo menos um número ('0'-'9').";
                    if (error.Description.Contains("non alphanumeric", StringComparison.OrdinalIgnoreCase) || error.Description.Contains("special", StringComparison.OrdinalIgnoreCase))
                        return "A senha deve conter pelo menos um caractere especial (!@#$...).";
                    if (error.Description.Contains("at least", StringComparison.OrdinalIgnoreCase) && error.Description.Contains("characters", StringComparison.OrdinalIgnoreCase))
                        return "A senha deve ter no mínimo 6 caracteres.";
                }

                return error.Description;
            }).ToList();
        }
    }
}
