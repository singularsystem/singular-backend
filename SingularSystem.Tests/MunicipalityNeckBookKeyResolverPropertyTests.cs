using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Security;
using SingularSystem.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: neckbook-books-proxy, Property 1: Resolução de município é consistente com os helpers
    // Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5, 5.6
    public sealed class MunicipalityNeckBookKeyResolverPropertyTests
    {
        private static readonly ApiKeyProtector Protector =
            new(new EphemeralDataProtectionProvider());

        public sealed record ConfigRowSpec(
            string State,
            string City,
            string PlaintextKey,
            bool IsActive);

        public sealed record ResolverScenario(
            string? QueryState,
            string? QueryCity,
            IReadOnlyList<ConfigRowSpec> Configs);

        public static class ResolverArbContainer
        {
            private static readonly (string Uf, string FullName)[] SampleStates =
            {
                ("PI", "Piauí"),
                ("SP", "São Paulo"),
                ("CE", "Ceará"),
                ("MA", "Maranhão"),
                ("BA", "Bahia")
            };

            private static readonly string[] SampleCities =
            {
                "Piripiri",
                "Teresina",
                "Parnaíba",
                "Fortaleza",
                "Campinas"
            };

            private static Gen<string> VariantStateGen((string Uf, string FullName) pair) =>
                Gen.Elements(
                    pair.Uf,
                    pair.Uf.ToLowerInvariant(),
                    $"  {pair.Uf} ",
                    pair.FullName,
                    pair.FullName.ToUpperInvariant(),
                    pair.FullName.ToLowerInvariant(),
                    $" {pair.FullName}  ");

            private static Gen<string> VariantCityGen(string city) =>
                Gen.Elements(
                    city,
                    city.ToUpperInvariant(),
                    city.ToLowerInvariant(),
                    $"  {city}  ",
                    $" {city.ToUpperInvariant()} ");

            public static Arbitrary<ResolverScenario> Scenario()
            {
                var rowGen =
                    from st in Gen.Elements(SampleStates)
                    from stText in VariantStateGen(st)
                    from city in Gen.Elements(SampleCities)
                    from cityText in VariantCityGen(city)
                    from isActive in Arb.Generate<bool>()
                    from suffix in Gen.Choose(1000, 999999)
                    select new ConfigRowSpec(stText, cityText, $"nk_live_key_{suffix}", isActive);

                var queryStateGen = Gen.Frequency(
                    Tuple.Create(2, Gen.Elements<string?>(null, "", "   ")),
                    Tuple.Create(8, from st in Gen.Elements(SampleStates)
                                    from v in VariantStateGen(st)
                                    select (string?)v));

                var queryCityGen = Gen.Frequency(
                    Tuple.Create(2, Gen.Elements<string?>(null, "", "   ")),
                    Tuple.Create(8, from c in Gen.Elements(SampleCities)
                                    from v in VariantCityGen(c)
                                    select (string?)v));

                var scenarioGen =
                    from qState in queryStateGen
                    from qCity in queryCityGen
                    from count in Gen.Choose(0, 6)
                    from rawRows in Gen.ListOf(count, rowGen)
                    let distinctRows = DeduplicateByMunicipality(rawRows)
                    select new ResolverScenario(qState, qCity, distinctRows);

                return Arb.From(scenarioGen);
            }

            private static IReadOnlyList<ConfigRowSpec> DeduplicateByMunicipality(IEnumerable<ConfigRowSpec> rows)
            {
                var result = new List<ConfigRowSpec>();
                foreach (var row in rows)
                {
                    var alreadyExists = result.Any(existing =>
                        Helpers.StateMatches(existing.State, row.State) &&
                        Helpers.CityMatches(existing.City, row.City));
                    if (!alreadyExists)
                    {
                        result.Add(row);
                    }
                }
                return result;
            }
        }

        // Feature: neckbook-books-proxy, Property 1: Resolução de município é consistente com os helpers
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ResolverArbContainer) })]
        public Property Resolver_returns_key_iff_active_match(ResolverScenario scenario)
        {
            using var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase($"resolver-p1-{Guid.NewGuid()}")
                    .Options);

            foreach (var row in scenario.Configs)
            {
                db.MunicipalityNeckBookConfigs.Add(new MunicipalityNeckBookConfig
                {
                    Id = Guid.NewGuid(),
                    State = row.State,
                    City = row.City,
                    EncryptedApiKey = Protector.Protect(row.PlaintextKey),
                    IsActive = row.IsActive
                });
            }
            db.SaveChanges();

            var resolver = new MunicipalityNeckBookKeyResolver(db, Protector);
            var actual = resolver.ResolveApiKeyAsync(scenario.QueryState, scenario.QueryCity).GetAwaiter().GetResult();

            if (string.IsNullOrWhiteSpace(scenario.QueryState) || string.IsNullOrWhiteSpace(scenario.QueryCity))
            {
                return (actual is null)
                    .Label($"Expected null when QueryState='{scenario.QueryState}' or QueryCity='{scenario.QueryCity}' is null/whitespace, got '{actual}'");
            }

            var matchingActive = scenario.Configs
                .Where(c => c.IsActive &&
                            Helpers.StateMatches(c.State, scenario.QueryState) &&
                            Helpers.CityMatches(c.City, scenario.QueryCity))
                .ToList();

            var expected = matchingActive.Count == 1 ? matchingActive[0].PlaintextKey : null;

            return (actual == expected)
                .Label($"Expected '{expected}', got '{actual}' for Query=({scenario.QueryState}, {scenario.QueryCity})");
        }
    }
}
