using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SingularSystem.Domain.Plans.Enums;
using SingularSystem.Models.Entities;

namespace SingularSystem.Services
{
    public class SubscriptionExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpirationService> _logger;

        public SubscriptionExpirationService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionExpirationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpirationsAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Erro ao processar expiração de assinaturas.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public async Task<(int Expired, int Suspended)> ProcessExpirationsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await ProcessExpirationsCoreAsync(db, DateTime.UtcNow, cancellationToken);
        }

        public async Task<(int Expired, int Suspended)> ProcessExpirationsCoreAsync(AppDbContext db, DateTime now, CancellationToken cancellationToken = default)
        {
            // Active -> PastDue
            var toExpire = await db.PlanSubscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate < now)
                .ToListAsync(cancellationToken);
            foreach (var sub in toExpire)
                sub.Status = SubscriptionStatus.PastDue;

            // PastDue -> Suspended (após 30 dias de grace period)
            var toSuspend = await db.PlanSubscriptions
                .Where(s => s.Status == SubscriptionStatus.PastDue && s.EndDate < now.AddDays(-30))
                .ToListAsync(cancellationToken);
            foreach (var sub in toSuspend)
                sub.Status = SubscriptionStatus.Suspended;

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "SubscriptionExpirationService: {Expired} expiradas, {Suspended} suspensas",
                toExpire.Count, toSuspend.Count);

            return (toExpire.Count, toSuspend.Count);
        }
    }
}
