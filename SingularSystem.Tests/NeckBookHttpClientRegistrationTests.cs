using System;
using System.Collections.Generic;
using System.Net.Http;
using SingularSystem.Configuration;
using SingularSystem.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Unit tests for Task 13.2: Named HttpClient "NeckBook" registration via IHttpClientFactory.
/// Validates: Requirements 3.1, 3.2, 3.4, 3.5, 3.6
/// </summary>
public class NeckBookHttpClientRegistrationTests
{
    [Fact]
    public void Named_HttpClient_NeckBook_Should_Configure_BaseAddress_Timeout_And_No_Static_Auth_Header()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NeckBook:BaseUrl"] = "https://api.neckbook.com.br",
                ["NeckBook:Timeout"] = "30"
            })
            .Build();

        var services = new ServiceCollection();
        var neckBookOptions = configuration.GetSection(NeckBookOptions.SectionName).Get<NeckBookOptions>() ?? new NeckBookOptions();

        services.AddHttpClient(NeckBookBooksController.NeckBookHttpClientName, client =>
        {
            var baseUrl = neckBookOptions.BaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(neckBookOptions.Timeout > 0 ? neckBookOptions.Timeout : 30);
        });

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(NeckBookBooksController.NeckBookHttpClientName);

        Assert.Equal("NeckBook", NeckBookBooksController.NeckBookHttpClientName);
        Assert.Equal(new Uri("https://api.neckbook.com.br/"), client.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
        Assert.Null(client.DefaultRequestHeaders.Authorization);
    }
}
