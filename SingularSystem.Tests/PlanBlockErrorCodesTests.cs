using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SingularSystem.Authorization;
using SingularSystem.Controllers;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Plans.Enums;
using SingularSystem.Domain.Tenancy;
using SingularSystem.DTOs.Students;
using SingularSystem.Filters;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-3-frontend-gating, Property 2: Regras de status de assinatura devolvem mensagem e código coerentes
    // Validates: Requirements 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 1.10, 1.12
    public sealed class PlanBlockErrorCodesTests
    {
        private sealed class FakePlanService : IPlanService
        {
            public QuotaResult QuotaResult { get; set; } = new(false, 10, 10);
            public Task<PlanSubscription?> GetActiveSubscriptionAsync(string tenantId) => Task.FromResult<PlanSubscription?>(null);
            public Task<PlanSubscription?> GetCurrentSubscriptionAsync(string tenantId) => Task.FromResult<PlanSubscription?>(null);
            public Task<QuotaResult> CheckStudentQuotaAsync(string tenantId) => Task.FromResult(QuotaResult);
            public Task<QuotaResult> CheckStudentQuotaAsync(string tenantId, int additional) => Task.FromResult(QuotaResult);
        }

        private static readonly string[] HttpMethods = new[]
        {
            "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"
        };

        [Property(MaxTest = 100)]
        public Property Subscription_status_rules_return_consistent_message_and_code(
            PositiveInt statusIdx,
            PositiveInt methodIdx)
        {
            var statuses = new[]
            {
                SubscriptionStatus.Active,
                SubscriptionStatus.PastDue,
                SubscriptionStatus.Suspended,
                SubscriptionStatus.Cancelled
            };

            var status = statuses[statusIdx.Get % statuses.Length];
            var method = HttpMethods[methodIdx.Get % HttpMethods.Length];
            var isWrite = SubscriptionStatusRules.IsWrite(method);

            var detailed = SubscriptionStatusRules.EvaluateDetailed(status, method);
            var simple = SubscriptionStatusRules.Evaluate(status, method);

            if (status == SubscriptionStatus.Active || (status == SubscriptionStatus.PastDue && !isWrite))
            {
                var allowed = detailed == null && simple == null;
                return allowed.ToProperty();
            }

            if (detailed == null || simple == null)
                return false.ToProperty();

            var messageMatches = detailed.Message == simple && !string.IsNullOrWhiteSpace(detailed.Message);

            var expectedCode = status switch
            {
                SubscriptionStatus.PastDue => PlanErrorCodes.SubscriptionPastDue,
                SubscriptionStatus.Suspended => PlanErrorCodes.SubscriptionSuspended,
                SubscriptionStatus.Cancelled => PlanErrorCodes.SubscriptionCancelled,
                _ => string.Empty
            };

            var codeMatches = detailed.ErrorCode == expectedCode;
            return (messageMatches && codeMatches).ToProperty();
        }

        [Fact]
        public async Task LockFilter_PastDue_Write_Returns_403_With_SubscriptionPastDue()
        {
            var tenantContext = new TenantContextService
            {
                TenantId = "tenant-1",
                Subscription = new PlanSubscription
                {
                    Status = SubscriptionStatus.PastDue
                }
            };
            var filter = new SubscriptionStatusLockFilter(tenantContext);
            var context = CreateActionContext("POST", "api/v1/resource", tenantContext);

            await filter.OnActionExecutionAsync(context.Context, () => Task.FromResult(new ActionExecutedContext(context.Context, new List<IFilterMetadata>(), new object())));

            var objResult = Assert.IsType<ObjectResult>(context.Context.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
            var apiResp = Assert.IsType<ApiResponse<object>>(objResult.Value);
            Assert.False(apiResp.Success);
            Assert.Equal(PlanErrorCodes.SubscriptionPastDue, apiResp.ErrorCode);
            Assert.Equal("Assinatura vencida. Apenas consulta é permitida até a regularização.", apiResp.Message);
        }

        [Fact]
        public async Task LockFilter_Suspended_Returns_403_With_SubscriptionSuspended()
        {
            var tenantContext = new TenantContextService
            {
                TenantId = "tenant-1",
                Subscription = new PlanSubscription
                {
                    Status = SubscriptionStatus.Suspended
                }
            };
            var filter = new SubscriptionStatusLockFilter(tenantContext);
            var context = CreateActionContext("GET", "api/v1/resource", tenantContext);

            await filter.OnActionExecutionAsync(context.Context, () => Task.FromResult(new ActionExecutedContext(context.Context, new List<IFilterMetadata>(), new object())));

            var objResult = Assert.IsType<ObjectResult>(context.Context.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
            var apiResp = Assert.IsType<ApiResponse<object>>(objResult.Value);
            Assert.Equal(PlanErrorCodes.SubscriptionSuspended, apiResp.ErrorCode);
            Assert.Equal("Assinatura suspensa. Regularize o pagamento para continuar.", apiResp.Message);
        }

        [Fact]
        public async Task LockFilter_Cancelled_Returns_403_With_SubscriptionCancelled()
        {
            var tenantContext = new TenantContextService
            {
                TenantId = "tenant-1",
                Subscription = new PlanSubscription
                {
                    Status = SubscriptionStatus.Cancelled
                }
            };
            var filter = new SubscriptionStatusLockFilter(tenantContext);
            var context = CreateActionContext("GET", "api/v1/resource", tenantContext);

            await filter.OnActionExecutionAsync(context.Context, () => Task.FromResult(new ActionExecutedContext(context.Context, new List<IFilterMetadata>(), new object())));

            var objResult = Assert.IsType<ObjectResult>(context.Context.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
            var apiResp = Assert.IsType<ApiResponse<object>>(objResult.Value);
            Assert.Equal(PlanErrorCodes.SubscriptionCancelled, apiResp.ErrorCode);
            Assert.Equal("Assinatura cancelada.", apiResp.Message);
        }

        [Fact]
        public async Task PlanFeatureAuthorize_FeatureUnavailable_Returns_403_With_FeatureUnavailable()
        {
            var tenantContext = new TenantContextService
            {
                IsPrivatePlan = true,
                TenantId = "tenant-1",
                Subscription = new PlanSubscription
                {
                    PlanType = PlanType.Standard,
                    Status = SubscriptionStatus.Active
                }
            };
            var attr = new PlanFeatureAuthorizeAttribute("Golden", "Empresa");
            var context = CreateActionContext("GET", "api/v1/reports", tenantContext, new FakePlanService());

            await attr.OnActionExecutionAsync(context.Context, () => Task.FromResult(new ActionExecutedContext(context.Context, new List<IFilterMetadata>(), new object())));

            var objResult = Assert.IsType<ObjectResult>(context.Context.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
            var apiResp = Assert.IsType<ApiResponse<object>>(objResult.Value);
            Assert.Equal(PlanErrorCodes.FeatureUnavailable, apiResp.ErrorCode);
            Assert.Equal("Recurso não disponível para o plano atual.", apiResp.Message);
        }

        [Fact]
        public async Task StudentsController_Create_ExceededQuota_Returns_402_With_QuotaExceeded_And_Data()
        {
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"students-quota-{Guid.NewGuid()}")
                .Options;
            using var db = new AppDbContext(dbOptions);

            var tenantContext = new TenantContextService
            {
                IsPrivatePlan = true,
                TenantId = "tenant-private"
            };

            var fakePlanService = new FakePlanService
            {
                QuotaResult = new QuotaResult(false, 10, 10)
            };

            var controller = new StudentsController(
                db,
                NullLogger<StudentsController>.Instance,
                null!,
                null!,
                tenantContext,
                fakePlanService);

            var model = new StudentRequest
            {
                FullName = "Aluno Teste",
                DateOfBirth = DateTime.Today.AddYears(-10),
                SchoolName = "Escola 1",
                SchoolCity = "Cidade",
                SchoolState = "SP"
            };

            var result = await controller.CreateStudent(model);

            var objResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status402PaymentRequired, objResult.StatusCode);
            var apiResp = Assert.IsType<ApiResponse<object>>(objResult.Value);
            Assert.Equal(PlanErrorCodes.QuotaExceeded, apiResp.ErrorCode);
            var quotaInfo = Assert.IsType<QuotaInfo>(apiResp.Data);
            Assert.Equal(10, quotaInfo.Current);
            Assert.Equal(10, quotaInfo.Max);
        }

        private static (ActionExecutingContext Context, bool[] NextCalled) CreateActionContext(
            string method,
            string? routeTemplate,
            ITenantContext tenantContext,
            IPlanService? planService = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton(tenantContext);
            if (planService != null)
                services.AddSingleton(planService);

            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };
            httpContext.Request.Method = method;

            var actionDescriptor = new ActionDescriptor();
            if (routeTemplate != null)
            {
                actionDescriptor.AttributeRouteInfo = new AttributeRouteInfo
                {
                    Template = routeTemplate
                };
            }

            var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object()
            );

            return (executingContext, new[] { false });
        }
    }
}

