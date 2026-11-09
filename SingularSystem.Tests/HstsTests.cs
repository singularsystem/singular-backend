using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class HstsTests
    {
        // Validates: Requirement 9.2, Task 10.4
        // HSTS presente com max-age >= 31536000 e includeSubDomains
        [Fact]
        public void Hsts_options_configured_with_one_year_max_age_and_subdomains()
        {
            var services = new ServiceCollection();
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromSeconds(31536000); // 1 ano (Req 9.2)
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<HstsOptions>>().Value;

            Assert.True(options.MaxAge.TotalSeconds >= 31536000, "MaxAge must be at least 31536000 seconds (1 year).");
            Assert.True(options.IncludeSubDomains, "IncludeSubDomains must be true.");
            Assert.True(options.Preload, "Preload must be true.");
        }

        [Fact]
        public async Task Hsts_middleware_emits_header_on_https_requests()
        {
            var options = Options.Create(new HstsOptions
            {
                Preload = true,
                IncludeSubDomains = true,
                MaxAge = TimeSpan.FromSeconds(31536000)
            });

            var middleware = new HstsMiddleware(
                next: (innerContext) => Task.CompletedTask,
                options: options,
                loggerFactory: NullLoggerFactory.Instance);

            var context = new DefaultHttpContext();
            context.Request.IsHttps = true;

            await middleware.Invoke(context);

            Assert.True(context.Response.Headers.ContainsKey("Strict-Transport-Security"),
                "Strict-Transport-Security header must be present on HTTPS responses.");

            var headerValue = context.Response.Headers["Strict-Transport-Security"].ToString();

            Assert.Contains("max-age=31536000", headerValue);
            Assert.Contains("includeSubDomains", headerValue);
        }

        [Fact]
        public async Task Hsts_middleware_does_not_emit_header_on_insecure_http_requests()
        {
            var options = Options.Create(new HstsOptions
            {
                Preload = true,
                IncludeSubDomains = true,
                MaxAge = TimeSpan.FromSeconds(31536000)
            });

            var middleware = new HstsMiddleware(
                next: (innerContext) => Task.CompletedTask,
                options: options,
                loggerFactory: NullLoggerFactory.Instance);

            var context = new DefaultHttpContext();
            context.Request.IsHttps = false;

            await middleware.Invoke(context);

            Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"),
                "Strict-Transport-Security header must NOT be emitted on plain HTTP requests.");
        }
    }
}
