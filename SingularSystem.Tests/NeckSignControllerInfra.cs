using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SingularSystem.Controllers;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace SingularSystem.Tests
{
    /// <summary>
    /// Infraestrutura dos testes do NeckSignController: contexto SQLite in-memory (impõe UNIQUE e
    /// suporta transações, necessário para a dedup transacional), cálculo de HMAC-SHA256 e
    /// construção do controller com um HttpContext cujo corpo/headers são controlados.
    /// </summary>
    internal sealed class NeckSignControllerHarness : IDisposable
    {
        public const string WebhookSecret = "necksign-webhook-secret-abc123";

        private readonly SqliteConnection _connection;
        public AppDbContext Context { get; }
        public NeckSignController Controller { get; }

        public NeckSignControllerHarness(string? secretOverride = null)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            Context = new AppDbContext(options);
            Context.Database.EnsureCreated();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NeckSign:WebhookSecret"] = secretOverride ?? WebhookSecret
                })
                .Build();

            var hub = new FakeHubContext();
            var notificationService = new NotificationService(
                Context, NullLogger<NotificationService>.Instance, hub, BuildUserManager());

            Controller = new NeckSignController(
                Context, NullLogger<NeckSignController>.Instance, notificationService, configuration);
        }

        /// <summary>Calcula o header de assinatura "sha256=&lt;hmac-hex&gt;" sobre o corpo bruto.</summary>
        public static string ComputeSignatureHeader(byte[] body, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(body);
            return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Invoca o endpoint Webhook com um corpo bruto e, opcionalmente, um header de assinatura.
        /// </summary>
        public async Task<IActionResult> PostWebhookAsync(byte[] body, string? signatureHeader)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(body);
            httpContext.Request.ContentLength = body.Length;
            httpContext.Request.ContentType = "application/json";
            if (signatureHeader is not null)
                httpContext.Request.Headers["X-NeckSign-Signature"] = signatureHeader;

            Controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return await Controller.Webhook();
        }

        private static UserManager<ApplicationUser> BuildUserManager()
        {
            return new UserManager<ApplicationUser>(
                new FakeUserStore(), null!, null!, null!, null!, null!, null!, null!, null!);
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }

        private sealed class FakeHubContext : IHubContext<NotificationHub>
        {
            public IHubClients Clients { get; } = new HubClients();
            public IGroupManager Groups { get; } = new GroupManager();

            private sealed class HubClients : IHubClients
            {
                private readonly IClientProxy _p = new Proxy();
                public IClientProxy All => _p;
                public IClientProxy AllExcept(IReadOnlyList<string> e) => _p;
                public IClientProxy Client(string c) => _p;
                public IClientProxy Clients(IReadOnlyList<string> c) => _p;
                public IClientProxy Group(string g) => _p;
                public IClientProxy GroupExcept(string g, IReadOnlyList<string> e) => _p;
                public IClientProxy Groups(IReadOnlyList<string> g) => _p;
                public IClientProxy User(string u) => _p;
                public IClientProxy Users(IReadOnlyList<string> u) => _p;
            }

            private sealed class Proxy : IClientProxy
            {
                public Task SendCoreAsync(string method, object?[] args, CancellationToken ct = default) => Task.CompletedTask;
            }

            private sealed class GroupManager : IGroupManager
            {
                public Task AddToGroupAsync(string c, string g, CancellationToken ct = default) => Task.CompletedTask;
                public Task RemoveFromGroupAsync(string c, string g, CancellationToken ct = default) => Task.CompletedTask;
            }
        }

        private sealed class FakeUserStore : IUserStore<ApplicationUser>
        {
            public void Dispose() { }
            public Task<string> GetUserIdAsync(ApplicationUser u, CancellationToken c) => Task.FromResult(u.Id);
            public Task<string?> GetUserNameAsync(ApplicationUser u, CancellationToken c) => Task.FromResult(u.UserName);
            public Task SetUserNameAsync(ApplicationUser u, string? n, CancellationToken c) => Task.CompletedTask;
            public Task<string?> GetNormalizedUserNameAsync(ApplicationUser u, CancellationToken c) => Task.FromResult(u.NormalizedUserName);
            public Task SetNormalizedUserNameAsync(ApplicationUser u, string? n, CancellationToken c) => Task.CompletedTask;
            public Task<IdentityResult> CreateAsync(ApplicationUser u, CancellationToken c) => Task.FromResult(IdentityResult.Success);
            public Task<IdentityResult> UpdateAsync(ApplicationUser u, CancellationToken c) => Task.FromResult(IdentityResult.Success);
            public Task<IdentityResult> DeleteAsync(ApplicationUser u, CancellationToken c) => Task.FromResult(IdentityResult.Success);
            public Task<ApplicationUser?> FindByIdAsync(string id, CancellationToken c) => Task.FromResult<ApplicationUser?>(null);
            public Task<ApplicationUser?> FindByNameAsync(string n, CancellationToken c) => Task.FromResult<ApplicationUser?>(null);
        }
    }
}
