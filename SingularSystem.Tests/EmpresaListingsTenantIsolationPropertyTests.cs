using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 11: Listagens de Empresa isoladas por tenant
    // Validates: Requirements 7.1, 7.3, 9.2, 9.4, 9.5, 9.6, 9.8
    public sealed class EmpresaListingsTenantIsolationPropertyTests
    {
        private static readonly string[] SchoolRoles = { "Teacher", "Psychologist", "Psychopedagogue", "SocialWork" };
        private static readonly string[] SampleCities = { "Piripiri", "Teresina", "Sao Paulo", "", "Desconhecida" };
        private static readonly string[] SampleStates = { "PI", "SP", "CE", "", "XX" };
        private static readonly string[] Tenants = { "tenant-alpha", "tenant-beta", "tenant-gamma" };

        private sealed class DummyTenantContext : ITenantContext
        {
            public string? TenantId { get; set; }
            public bool IsPrivatePlan { get; set; }
            public SingularSystem.Domain.Plans.PlanSubscription? Subscription => null;
        }

        private static List<T> CreateSampleList<T>(
            Func<Student, T> factory,
            string targetTenant,
            int countPerTenant) where T : BaseDocument
        {
            var list = new List<T>();
            var allTenants = new[] { targetTenant, "tenant-other-1", "tenant-other-2", null, "" };

            foreach (var t in allTenants)
            {
                for (int i = 0; i < countPerTenant; i++)
                {
                    var student = new Student
                    {
                        ID = Guid.NewGuid(),
                        TenantId = t,
                        City = "StudentCity" + i,
                        State = "PI",
                        IsActive = true
                    };
                    var doc = factory(student);
                    doc.Student = student;
                    doc.StudentID = student.ID;
                    list.Add(doc);
                }
            }

            return list;
        }

        [Property(MaxTest = 100)]
        public Property Empresa_listings_are_isolated_by_tenant_in_private_plan(
            PositiveInt roleIdx,
            PositiveInt cityIdx,
            PositiveInt stateIdx,
            PositiveInt tenantIdx)
        {
            var role = SchoolRoles[roleIdx.Get % SchoolRoles.Length];
            var userCity = SampleCities[cityIdx.Get % SampleCities.Length];
            var userState = SampleStates[stateIdx.Get % SampleStates.Length];
            var targetTenant = Tenants[tenantIdx.Get % Tenants.Length];

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                City = userCity,
                State = userState
            };

            var tenantContext = new DummyTenantContext
            {
                IsPrivatePlan = true,
                TenantId = targetTenant
            };

            // Test across representative inventory entities
            var carsList = CreateSampleList(s => new CARSQuestionnaire { ID = Guid.NewGuid() }, targetTenant, 2);
            var adaptiveList = CreateSampleList(s => new AdaptiveBehaviorScaleDI { ID = Guid.NewGuid() }, targetTenant, 2);
            var cognitiveList = CreateSampleList(s => new CognitiveAssessment { ID = Guid.NewGuid() }, targetTenant, 2);
            var mchatList = CreateSampleList(s => new MCHATQuestionnaire { ID = Guid.NewGuid() }, targetTenant, 2);
            var snapList = CreateSampleList(s => new SnapIVQuestionnaire { ID = Guid.NewGuid() }, targetTenant, 2);
            var psychQList = CreateSampleList(s => new PsychologistQuestionnaire { ID = Guid.NewGuid() }, targetTenant, 2);
            var psychEdQList = CreateSampleList(s => new PsychopedagogueQuestionnaire { ID = Guid.NewGuid() }, targetTenant, 2);
            var psychRList = CreateSampleList(s => new PsychologistReport { ID = Guid.NewGuid() }, targetTenant, 2);
            var psychEdRList = CreateSampleList(s => new PsychopedagogueReport { ID = Guid.NewGuid() }, targetTenant, 2);
            var socialRList = CreateSampleList(s => new SocialWorkerReport { ID = Guid.NewGuid() }, targetTenant, 2);
            var portageList = CreateSampleList(s => new PortageGuide { ID = Guid.NewGuid() }, targetTenant, 2);

            bool VerifyEntityIsolation<T>(List<T> list) where T : BaseDocument
            {
                var query = list.AsQueryable().WhereStudentInTenant(tenantContext);

                // Controller branch: if (TenantContext?.IsPrivatePlan != true && !isAdmin) apply geo check
                // In Private Plan, this condition is FALSE, so the geo check is bypassed
                bool isBypassed = !(tenantContext.IsPrivatePlan != true);

                var results = query.ToList();

                var allTargetTenant = results.All(r => r.Student != null && r.Student.TenantId == targetTenant);
                var expectedCount = list.Count(r => r.Student?.TenantId == targetTenant);
                var exactCount = results.Count == expectedCount;

                return isBypassed && allTargetTenant && exactCount;
            }

            var ok = VerifyEntityIsolation(carsList)
                  && VerifyEntityIsolation(adaptiveList)
                  && VerifyEntityIsolation(cognitiveList)
                  && VerifyEntityIsolation(mchatList)
                  && VerifyEntityIsolation(snapList)
                  && VerifyEntityIsolation(psychQList)
                  && VerifyEntityIsolation(psychEdQList)
                  && VerifyEntityIsolation(psychRList)
                  && VerifyEntityIsolation(psychEdRList)
                  && VerifyEntityIsolation(socialRList)
                  && VerifyEntityIsolation(portageList);

            return ok.ToProperty();
        }
    }
}
