using System.Reflection;
using System.Security.Claims;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SingularSystem.Authorization;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Plans.Enums;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Filters;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class SubscriptionStatusLockFilterPropertyTests
    {
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

            var nextCalled = new bool[] { false };
            return (executingContext, nextCalled);
        }

        private static ActionExecutionDelegate CreateNext(bool[] nextCalled, ActionExecutingContext context)
        {
            return () =>
            {
                nextCalled[0] = true;
                return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), context.Controller));
            };
        }

        private static readonly string[] NonExemptRoutes =
        {
            "api/v1/students",
            "api/v1/peis",
            "api/v1/paees",
            "api/v1/reports",
            "api/v1/cars-questionnaires"
        };

        private static readonly string[] ExemptRoutes =
        {
            "api/v1/users/login",
            "api/v1/users/logout",
            "api/v1/users/me"
        };

        private static readonly string[] AllMethods = { "GET", "HEAD", "OPTIONS", "POST", "PUT", "PATCH", "DELETE" };
        private static readonly string[] WriteMethods = { "POST", "PUT", "PATCH", "DELETE" };
        private static readonly string[] ReadMethods = { "GET", "HEAD", "OPTIONS" };

        // Feature: modulo-3-2-travas-backend, Property 2: Suspended e Cancelled bloqueiam tudo, exceto rotas de exceção
        // Validates: Requirements 1.3, 10.1, 10.2
        [Property(MaxTest = 100)]
        public Property Suspended_e_Cancelled_bloqueiam_tudo_exceto_excecao(bool isCancelled, PositiveInt methodIdx, PositiveInt routeIdx)
        {
            var status = isCancelled ? SubscriptionStatus.Cancelled : SubscriptionStatus.Suspended;
            var method = AllMethods[methodIdx.Get % AllMethods.Length];
            var nonExemptRoute = NonExemptRoutes[routeIdx.Get % NonExemptRoutes.Length];
            var exemptRoute = ExemptRoutes[routeIdx.Get % ExemptRoutes.Length];

            var tenantContext = new TenantContextService
            {
                TenantId = "tenant-1",
                IsPrivatePlan = true,
                Subscription = new PlanSubscription
                {
                    Status = status,
                    PlanType = PlanType.Standard
                }
            };
            var filter = new SubscriptionStatusLockFilter(tenantContext);

            // 1. Rota normal deve bloquear com 403
            var (ctx1, next1) = CreateActionContext(method, nonExemptRoute, tenantContext);
            filter.OnActionExecutionAsync(ctx1, CreateNext(next1, ctx1)).GetAwaiter().GetResult();
            bool normalBlocked = !next1[0] && ctx1.Result is ObjectResult { StatusCode: StatusCodes.Status403Forbidden };

            // 2. Rota de exceção deve passar
            var (ctx2, next2) = CreateActionContext(method, exemptRoute, tenantContext);
            filter.OnActionExecutionAsync(ctx2, CreateNext(next2, ctx2)).GetAwaiter().GetResult();
            bool exemptPassed = next2[0] && ctx2.Result == null;

            return (normalBlocked && exemptPassed).ToProperty()
                .Label($"status={status}, method={method}, normalBlocked={normalBlocked}, exemptPassed={exemptPassed}");
        }

        // Feature: modulo-3-2-travas-backend, Property 3: PastDue bloqueia escrita em qualquer rota e libera leitura
        // Validates: Requirements 1.4, 10.1
        [Property(MaxTest = 100)]
        public Property PastDue_bloqueia_escrita_e_libera_leitura(PositiveInt writeIdx, PositiveInt readIdx, PositiveInt routeIdx)
        {
            var writeMethod = WriteMethods[writeIdx.Get % WriteMethods.Length];
            var readMethod = ReadMethods[readIdx.Get % ReadMethods.Length];
            var route = NonExemptRoutes[routeIdx.Get % NonExemptRoutes.Length];

            var tenantContext = new TenantContextService
            {
                TenantId = "tenant-1",
                IsPrivatePlan = true,
                Subscription = new PlanSubscription
                {
                    Status = SubscriptionStatus.PastDue,
                    PlanType = PlanType.Standard
                }
            };
            var filter = new SubscriptionStatusLockFilter(tenantContext);

            // Escrita => 403
            var (ctxWrite, nextWrite) = CreateActionContext(writeMethod, route, tenantContext);
            filter.OnActionExecutionAsync(ctxWrite, CreateNext(nextWrite, ctxWrite)).GetAwaiter().GetResult();
            bool writeBlocked = !nextWrite[0] && ctxWrite.Result is ObjectResult { StatusCode: StatusCodes.Status403Forbidden };

            // Leitura => libera
            var (ctxRead, nextRead) = CreateActionContext(readMethod, route, tenantContext);
            filter.OnActionExecutionAsync(ctxRead, CreateNext(nextRead, ctxRead)).GetAwaiter().GetResult();
            bool readAllowed = nextRead[0] && ctxRead.Result == null;

            return (writeBlocked && readAllowed).ToProperty()
                .Label($"write={writeMethod} (blocked={writeBlocked}), read={readMethod} (allowed={readAllowed})");
        }

        // Feature: modulo-3-2-travas-backend, Property 4: Assinatura Active nunca é bloqueada pela trava
        // Validates: Requirements 10.1
        [Property(MaxTest = 100)]
        public Property Assinatura_Active_nunca_e_bloqueada(PositiveInt methodIdx, PositiveInt routeIdx)
        {
            var method = AllMethods[methodIdx.Get % AllMethods.Length];
            var route = NonExemptRoutes[routeIdx.Get % NonExemptRoutes.Length];

            var tenantContext = new TenantContextService
            {
                TenantId = "tenant-1",
                IsPrivatePlan = true,
                Subscription = new PlanSubscription
                {
                    Status = SubscriptionStatus.Active,
                    PlanType = PlanType.Standard
                }
            };
            var filter = new SubscriptionStatusLockFilter(tenantContext);

            var (ctx, next) = CreateActionContext(method, route, tenantContext);
            filter.OnActionExecutionAsync(ctx, CreateNext(next, ctx)).GetAwaiter().GetResult();

            bool passed = next[0] && ctx.Result == null;
            return passed.ToProperty().Label($"method={method}, route={route}, passed={passed}");
        }

        // Feature: modulo-3-2-travas-backend, Property 5: Público legado não é afetado pela trava global
        // Validates: Requirements 10.3
        [Property(MaxTest = 100)]
        public Property Publico_legado_nao_e_afetado(PositiveInt methodIdx, PositiveInt routeIdx)
        {
            var method = AllMethods[methodIdx.Get % AllMethods.Length];
            var route = NonExemptRoutes[routeIdx.Get % NonExemptRoutes.Length];

            var tenantContext = new TenantContextService
            {
                TenantId = "tenant-legacy",
                IsPrivatePlan = false,
                Subscription = null
            };
            var filter = new SubscriptionStatusLockFilter(tenantContext);

            var (ctx, next) = CreateActionContext(method, route, tenantContext);
            filter.OnActionExecutionAsync(ctx, CreateNext(next, ctx)).GetAwaiter().GetResult();

            bool passed = next[0] && ctx.Result == null;
            return passed.ToProperty().Label($"method={method}, route={route}, passed={passed}");
        }

        private sealed class FakePlanService : IPlanService
        {
            private readonly PlanSubscription? _sub;
            public FakePlanService(PlanSubscription? sub) => _sub = sub;
            public Task<PlanSubscription?> GetActiveSubscriptionAsync(string tenantId) => Task.FromResult(_sub?.Status == SubscriptionStatus.Active ? _sub : null);
            public Task<PlanSubscription?> GetCurrentSubscriptionAsync(string tenantId) => Task.FromResult(_sub);
            public Task<QuotaResult> CheckStudentQuotaAsync(string tenantId) => Task.FromResult(new QuotaResult(true, 0, 0));
            public Task<QuotaResult> CheckStudentQuotaAsync(string tenantId, int additional) => Task.FromResult(new QuotaResult(true, 0, 0));
        }

        // Feature: modulo-3-2-travas-backend, Property 7: PlanFeatureAuthorize decide por PlanType e por IsPrivatePlan
        // Validates: Requirements 1.2, 1.5
        [Property(MaxTest = 100)]
        public Property PlanFeatureAuthorize_decide_por_PlanType_e_IsPrivatePlan(PositiveInt planTypeIdx, bool isPrivate)
        {
            var planType = (PlanType)(planTypeIdx.Get % 4); // Standard, Golden, Empresa, Publico
            var requiredPlans = new[] { "Empresa", "Publico" };

            var sub = isPrivate ? new PlanSubscription
            {
                PlanType = planType,
                Status = SubscriptionStatus.Active
            } : null;

            var tenantContext = new TenantContextService
            {
                TenantId = "t1",
                IsPrivatePlan = isPrivate,
                Subscription = sub
            };
            var planService = new FakePlanService(sub);

            var attr = new PlanFeatureAuthorizeAttribute(requiredPlans);
            var (ctx, next) = CreateActionContext("GET", "api/v1/reports", tenantContext, planService);

            attr.OnActionExecutionAsync(ctx, CreateNext(next, ctx)).GetAwaiter().GetResult();

            bool expectedAllowed;
            if (!isPrivate)
            {
                // Público legado: permitido se "Publico" estiver em requiredPlans
                expectedAllowed = requiredPlans.Contains("Publico");
            }
            else
            {
                // Privado: permitido se o tipo da assinatura estiver em requiredPlans
                expectedAllowed = requiredPlans.Contains(planType.ToString());
            }

            bool passed = next[0] && ctx.Result == null;
            bool blocked = !next[0] && ctx.Result is ObjectResult { StatusCode: StatusCodes.Status403Forbidden };

            bool correctlyDecided = expectedAllowed ? passed : blocked;
            return correctlyDecided.ToProperty()
                .Label($"isPrivate={isPrivate}, plan={planType}, expected={expectedAllowed}, passed={passed}, blocked={blocked}");
        }

        // Feature: modulo-3-2-travas-backend, Property 8: Defesa em profundidade equivale à trava global
        // Validates: Requirements 1.3, 1.4, 10.4
        [Property(MaxTest = 100)]
        public Property Defesa_em_profundidade_equivale_a_trava_global(PositiveInt statusIdx, PositiveInt methodIdx)
        {
            var status = (SubscriptionStatus)(statusIdx.Get % 5); // None, Active, PastDue, Suspended, Cancelled
            var method = AllMethods[methodIdx.Get % AllMethods.Length];

            // Avaliação direta do SubscriptionStatusRules
            var eval = SubscriptionStatusRules.Evaluate(status, method);

            // Simula contexto com PlanFeatureAuthorizeAttribute
            var sub = new PlanSubscription
            {
                PlanType = PlanType.Empresa,
                Status = status
            };
            var tenantContext = new TenantContextService
            {
                TenantId = "t1",
                IsPrivatePlan = true,
                Subscription = sub
            };
            var planService = new FakePlanService(sub);
            var attr = new PlanFeatureAuthorizeAttribute("Empresa");

            var (ctx, next) = CreateActionContext(method, "api/v1/reports", tenantContext, planService);
            attr.OnActionExecutionAsync(ctx, CreateNext(next, ctx)).GetAwaiter().GetResult();

            if (eval is not null)
            {
                // Deve bloquear com 403 e a mesma mensagem
                bool blockedWithSameMessage = !next[0] &&
                    ctx.Result is ObjectResult obj &&
                    obj.StatusCode == StatusCodes.Status403Forbidden &&
                    obj.Value is Models.ApiResponse<object> apiResp &&
                    apiResp.Message == eval;

                return blockedWithSameMessage.ToProperty().Label($"status={status}, method={method}, eval='{eval}'");
            }
            else
            {
                bool passed = next[0] && ctx.Result == null;
                return passed.ToProperty().Label($"status={status}, method={method}, passed={passed}");
            }
        }

        // Feature: modulo-3-2-travas-backend, Property 18: Componentes de decisão de plano não acessam AppDbContext
        // Validates: Requirements 9.1
        [Fact]
        public void Componentes_de_decisao_nao_acessam_AppDbContext()
        {
            // SubscriptionStatusRules é estático puro
            var rulesFields = typeof(SubscriptionStatusRules).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.Empty(rulesFields);

            // SubscriptionStatusLockFilter não deve ter AppDbContext como campo ou no construtor
            var filterCtors = typeof(SubscriptionStatusLockFilter).GetConstructors();
            foreach (var ctor in filterCtors)
            {
                var paramTypes = ctor.GetParameters().Select(p => p.ParameterType);
                Assert.DoesNotContain(typeof(AppDbContext), paramTypes);
            }
            var filterFields = typeof(SubscriptionStatusLockFilter).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.DoesNotContain(filterFields, f => f.FieldType == typeof(AppDbContext));

            // PlanFeatureAuthorizeAttribute não deve ter AppDbContext como campo ou construtor
            var attrCtors = typeof(PlanFeatureAuthorizeAttribute).GetConstructors();
            foreach (var ctor in attrCtors)
            {
                var paramTypes = ctor.GetParameters().Select(p => p.ParameterType);
                Assert.DoesNotContain(typeof(AppDbContext), paramTypes);
            }
            var attrFields = typeof(PlanFeatureAuthorizeAttribute).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.DoesNotContain(attrFields, f => f.FieldType == typeof(AppDbContext));
        }
    }
}
