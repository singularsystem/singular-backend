using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SingularSystem.Controllers;
using SingularSystem.Domain.Access;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;

namespace SingularSystem.Tests
{
    public sealed class StudentAccessPolicyPropertyTests
    {
        private static readonly string[] RolesSeed =
        {
            "Admin", "Secretary", "SocialWork", "Psychologist", "Psychopedagogue",
            "Teacher", "Coordinator", "Principal", "MunicipalManager",
            "AutonomousProfessional", "SchoolAdmin"
        };

        private static readonly string[] SmallCities = { "Piripiri", "Teresina", "Parnaiba", "Fortaleza" };
        private static readonly string[] SmallStates = { "PI", "Piaui", "CE", "Ceara" };
        private static readonly string[] Tenants = { "tenant-alpha", "tenant-beta", "tenant-gamma" };

        private sealed class TestTenantContext : ITenantContext
        {
            public bool IsPrivatePlan { get; set; }
            public string? TenantId { get; set; }
            public PlanSubscription? Subscription { get; set; }
        }

        private sealed class TestController : BaseController
        {
            public TestController(IStudentAccessPolicy policy, ITenantContext? tenant)
            {
                var services = new ServiceCollection();
                services.AddSingleton(policy);
                if (tenant != null)
                {
                    services.AddSingleton(tenant);
                }

                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = services.BuildServiceProvider()
                    }
                };
            }

            public bool InvokeHasStudentAccess(ApplicationUser user, Student student, IList<string> roles)
                => HasStudentAccess(user, student, roles);
        }

        // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 1: Equivalência da política com HasStudentAccess anterior
        // Validates: Requirements 3.2, 3.3, 4.4, 4.5, 4.6, 4.7
        [Property(MaxTest = 100)]
        public bool Policy_matches_legacy_has_student_access_and_base_controller(
            int roleIdx,
            int userCityIdx,
            int userStateIdx,
            int studentCityIdx,
            int studentStateIdx,
            int schoolCityIdx,
            int schoolStateIdx,
            bool hasSchool,
            bool hasSchoolIdOnUser,
            bool hasSchoolIdOnStudent,
            bool sameSchoolId,
            int tenantContextMode, // 0 = null, 1 = public, 2 = private matching, 3 = private different
            int studentTenantMode // 0 = null, 1 = matching, 2 = other
        )
        {
            var role = RolesSeed[Math.Abs(roleIdx) % RolesSeed.Length];
            var roles = new List<string> { role };

            var userCity = SmallCities[Math.Abs(userCityIdx) % SmallCities.Length];
            var userState = SmallStates[Math.Abs(userStateIdx) % SmallStates.Length];

            var studentCity = SmallCities[Math.Abs(studentCityIdx) % SmallCities.Length];
            var studentState = SmallStates[Math.Abs(studentStateIdx) % SmallStates.Length];

            var schoolCity = SmallCities[Math.Abs(schoolCityIdx) % SmallCities.Length];
            var schoolState = SmallStates[Math.Abs(schoolStateIdx) % SmallStates.Length];

            var sharedSchoolGuid = Guid.NewGuid();
            var userSchoolId = hasSchoolIdOnUser ? (sameSchoolId ? sharedSchoolGuid : Guid.NewGuid()) : (Guid?)null;
            var studentSchoolId = hasSchoolIdOnStudent ? (sameSchoolId ? sharedSchoolGuid : Guid.NewGuid()) : (Guid?)null;

            var activeTenant = Tenants[0];
            ITenantContext? tenantContext = (Math.Abs(tenantContextMode) % 4) switch
            {
                0 => null,
                1 => new TestTenantContext { IsPrivatePlan = false, TenantId = activeTenant },
                2 => new TestTenantContext { IsPrivatePlan = true, TenantId = activeTenant },
                _ => new TestTenantContext { IsPrivatePlan = true, TenantId = Tenants[1] }
            };

            string? studentTenantId = (Math.Abs(studentTenantMode) % 3) switch
            {
                0 => null,
                1 => activeTenant,
                _ => Tenants[2]
            };

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                City = userCity,
                State = userState,
                SchoolID = userSchoolId,
                TenantId = tenantContext?.IsPrivatePlan == true ? tenantContext.TenantId : null
            };

            var student = new Student
            {
                ID = Guid.NewGuid(),
                City = studentCity,
                State = studentState,
                SchoolID = studentSchoolId,
                TenantId = studentTenantId,
                School = hasSchool ? new School
                {
                    ID = studentSchoolId ?? Guid.NewGuid(),
                    City = schoolCity,
                    State = schoolState
                } : null
            };

            var policy = new StudentAccessPolicy();
            var policyResult = policy.CanAccess(user, student, roles, tenantContext);

            var legacyResult = LegacyAccessHelper.LegacyHasStudentAccess(user, student, roles, tenantContext);

            var controller = new TestController(policy, tenantContext);
            var controllerResult = controller.InvokeHasStudentAccess(user, student, roles);

            return policyResult == legacyResult && controllerResult == policyResult;
        }
    }
}
