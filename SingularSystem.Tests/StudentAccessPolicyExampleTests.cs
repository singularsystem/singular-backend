using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SingularSystem.Controllers;
using SingularSystem.Domain.Access;
using SingularSystem.Extensions;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class StudentAccessPolicyExampleTests
    {
        private sealed class TestController : BaseController
        {
            public TestController(IServiceProvider sp)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = sp
                    }
                };
            }

            public bool InvokeHasStudentAccess(ApplicationUser user, Student student, IList<string> roles)
                => HasStudentAccess(user, student, roles);
        }

        private sealed class CapturingLoggerFactory : ILoggerFactory
        {
            public List<(LogLevel Level, string Category, string Message)> Logs { get; } = new();

            public void AddProvider(ILoggerProvider provider) { }

            public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

            public void Dispose() { }

            private sealed class CapturingLogger : ILogger
            {
                private readonly CapturingLoggerFactory _factory;
                private readonly string _category;

                public CapturingLogger(CapturingLoggerFactory factory, string category)
                {
                    _factory = factory;
                    _category = category;
                }

                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(
                    LogLevel logLevel,
                    EventId eventId,
                    TState state,
                    Exception? exception,
                    Func<TState, Exception?, string> formatter)
                {
                    _factory.Logs.Add((logLevel, _category, formatter(state, exception)));
                }
            }
        }

        [Fact]
        public void Missing_policy_in_di_returns_false_and_logs_error()
        {
            var loggerFactory = new CapturingLoggerFactory();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(loggerFactory);
            var sp = services.BuildServiceProvider();

            var controller = new TestController(sp);
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), City = "Piripiri", State = "PI" };
            var student = new Student { ID = Guid.NewGuid(), City = "Piripiri", State = "PI" };
            var roles = new List<string> { "Teacher" };

            var allowed = controller.InvokeHasStudentAccess(user, student, roles);

            Assert.False(allowed);
            Assert.Contains(loggerFactory.Logs, log =>
                log.Level == LogLevel.Error &&
                log.Category == "StudentAccess" &&
                log.Message.Contains("IStudentAccessPolicy não registrada"));
        }

        [Fact]
        public void Interface_has_no_http_or_dbcontext_references_and_is_scoped()
        {
            var interfaceType = typeof(IStudentAccessPolicy);

            Assert.StartsWith("SingularSystem.Domain.", interfaceType.Namespace!);

            var method = interfaceType.GetMethod("CanAccess")!;
            Assert.NotNull(method);

            var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToList();

            foreach (var type in paramTypes)
            {
                var full = type.FullName ?? "";
                Assert.DoesNotContain("HttpContext", full);
                Assert.DoesNotContain("ControllerBase", full);
                Assert.DoesNotContain("DbContext", full);
            }

            var services = new ServiceCollection();
            services.AddPlanServices();

            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IStudentAccessPolicy));
            Assert.NotNull(descriptor);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
            Assert.Equal(typeof(StudentAccessPolicy), descriptor.ImplementationType);
        }
    }
}
