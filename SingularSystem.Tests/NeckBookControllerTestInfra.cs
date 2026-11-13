using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using SingularSystem.Controllers;
using SingularSystem.Domain.Access;
using SingularSystem.Models.Entities;
using SingularSystem.Security;
using SingularSystem.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SingularSystem.Tests
{
    /// <summary>
    /// Infraestrutura de testes para <see cref="NeckBookBooksController"/> e <see cref="NeckBookAdminController"/>:
    /// UserManager in-memory, logger capturador de entradas e fábrica de controller com <see cref="ClaimsPrincipal"/>.
    /// </summary>
    internal static class NeckBookControllerTestInfra
    {
        public static readonly ApiKeyProtector Protector =
            new(new EphemeralDataProtectionProvider());

        public static AppDbContext CreateContext(string? dbName = null) =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName ?? $"neckbook-ctrl-{Guid.NewGuid()}")
                .Options);

        public static UserManager<ApplicationUser> CreateUserManager(
            AppDbContext db,
            IDictionary<string, IList<string>> rolesByUserId)
        {
            var store = new InMemoryUserRoleStore(db, rolesByUserId);
            return new UserManager<ApplicationUser>(
                store, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        public static NeckBookBooksController CreateBooksController(
            AppDbContext db,
            ApplicationUser? authenticatedUser,
            IList<string> roles,
            HttpMessageHandler handler,
            CapturingLogger<NeckBookBooksController>? logger = null)
        {
            var rolesMap = new Dictionary<string, IList<string>>();
            if (authenticatedUser is not null)
            {
                rolesMap[authenticatedUser.Id] = roles;
            }

            var userManager = CreateUserManager(db, rolesMap);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.neckbook.com.br/")
            };
            var factory = new SingleClientFactory(httpClient);
            var resolver = new MunicipalityNeckBookKeyResolver(db, Protector);
            var assembler = new NeckBookPayloadAssembler(db);
            var effectiveLogger = logger ?? new CapturingLogger<NeckBookBooksController>();

            var controller = new NeckBookBooksController(
                factory,
                userManager,
                db,
                resolver,
                assembler,
                effectiveLogger);

            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddSingleton<IStudentAccessPolicy, StudentAccessPolicy>();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider()
            };
            if (authenticatedUser is not null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, authenticatedUser.Id)
                };
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            }
            else
            {
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }
    }

    internal sealed class InMemoryUserRoleStore : IUserRoleStore<ApplicationUser>
    {
        private readonly AppDbContext _db;
        private readonly IDictionary<string, IList<string>> _rolesByUserId;

        public InMemoryUserRoleStore(AppDbContext db, IDictionary<string, IList<string>> rolesByUserId)
        {
            _db = db;
            _rolesByUserId = rolesByUserId;
        }

        public void Dispose() { }
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) { user.UserName = userName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);

        public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            await _db.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);

        public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (_rolesByUserId.TryGetValue(user.Id, out var roles))
            {
                return Task.FromResult<IList<string>>(roles.ToList());
            }
            return Task.FromResult<IList<string>>(new List<string>());
        }

        public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            var hasRole = _rolesByUserId.TryGetValue(user.Id, out var roles) && roles.Contains(roleName);
            return Task.FromResult(hasRole);
        }

        public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken) =>
            Task.FromResult<IList<ApplicationUser>>(new List<ApplicationUser>());
    }

    internal sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly List<string> _entries = new();

        public IReadOnlyList<string> Entries => _entries;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var msg = formatter(state, exception);
            if (exception is not null)
            {
                msg += " | " + exception;
            }
            _entries.Add($"[{logLevel}] {msg}");
        }
    }
}
