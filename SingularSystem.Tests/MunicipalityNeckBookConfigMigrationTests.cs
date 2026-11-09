using System;
using System.Linq;
using System.Threading.Tasks;
using SingularSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: neckbook-books-proxy — Task 4.4: testes unitários do índice único (State, City) em MunicipalityNeckBookConfig
    // Validates: Requirements 4.1, 4.2, 4.6
    public sealed class MunicipalityNeckBookConfigMigrationTests
    {
        private static AppDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"neckbook-config-{Guid.NewGuid()}")
                .Options);

        [Fact]
        public void MunicipalityNeckBookConfig_declares_unique_index_on_State_and_City()
        {
            using var context = CreateContext();

            var entityType = context.Model.FindEntityType(typeof(MunicipalityNeckBookConfig));
            Assert.NotNull(entityType);

            var uniqueIndex = entityType!.GetIndexes().FirstOrDefault(i => i.IsUnique);
            Assert.NotNull(uniqueIndex);

            var columns = uniqueIndex!.Properties.Select(p => p.Name).ToArray();
            Assert.Equal(
                new[]
                {
                    nameof(MunicipalityNeckBookConfig.State),
                    nameof(MunicipalityNeckBookConfig.City)
                },
                columns);
        }

        [Fact]
        public async Task MunicipalityNeckBookConfig_allows_distinct_state_city_pairs_and_defaults_IsActive_true()
        {
            using var context = CreateContext();

            var first = new MunicipalityNeckBookConfig
            {
                State = "Piauí",
                City = "Piripiri",
                EncryptedApiKey = "cipher-1"
            };
            var second = new MunicipalityNeckBookConfig
            {
                State = "Piauí",
                City = "Teresina",
                EncryptedApiKey = "cipher-2"
            };

            context.MunicipalityNeckBookConfigs.AddRange(first, second);
            await context.SaveChangesAsync();

            Assert.True(first.IsActive);
            Assert.Equal(2, await context.MunicipalityNeckBookConfigs.CountAsync());
        }
    }
}
