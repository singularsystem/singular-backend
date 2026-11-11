using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Spike 1.3: Verificação de UseForwardedHeaders e política de limite de taxa.
/// Validates: Requirement 12.9
/// </summary>
public class ForwardedHeadersAndRateLimitingSmokeTests
{
    [Fact]
    public async Task ForwardedHeadersMiddleware_Resolves_RemoteIp_From_XForwardedFor()
    {
        var options = Options.Create(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        options.Value.KnownNetworks.Clear();
        options.Value.KnownProxies.Clear();

        IPAddress? capturedRemoteIp = null;

        var middleware = new ForwardedHeadersMiddleware(
            next: (ctx) =>
            {
                capturedRemoteIp = ctx.Connection.RemoteIpAddress;
                return Task.CompletedTask;
            },
            loggerFactory: NullLoggerFactory.Instance,
            options: options);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.195";
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        await middleware.Invoke(context);

        Assert.NotNull(capturedRemoteIp);
        Assert.Equal("203.0.113.195", capturedRemoteIp.ToString());
        Assert.Equal("https", context.Request.Scheme);
    }
}
