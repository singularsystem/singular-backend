using System;
using System.Collections.Generic;
using System.IO;
using SingularSystem.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: neckbook-books-proxy — Task 1.3: testes unitários do NeckBookStartupValidator (fail-fast)
    // Validates: Requirements 2.1, 2.2, 2.3, 2.4
    public sealed class NeckBookStartupValidatorTests
    {
        private static IConfiguration BuildConfiguration(string? baseUrl)
        {
            var dict = new Dictionary<string, string?>();
            if (baseUrl is not null)
            {
                dict["NeckBook:BaseUrl"] = baseUrl;
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();
        }

        [Fact]
        public void Validate_fails_when_BaseUrl_is_absent()
        {
            var config = BuildConfiguration(null);
            using var err = new StringWriter();

            var ok = NeckBookStartupValidator.Validate(config, err);

            Assert.False(ok);
            Assert.Contains("NeckBook:BaseUrl", err.ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_fails_when_BaseUrl_is_empty_or_whitespace(string rawValue)
        {
            var config = BuildConfiguration(rawValue);
            using var err = new StringWriter();

            var ok = NeckBookStartupValidator.Validate(config, err);

            Assert.False(ok);
            Assert.Contains("NeckBook:BaseUrl", err.ToString());
        }

        [Theory]
        [InlineData("/v1/books")]
        [InlineData("api.neckbook.com.br")]
        [InlineData("not a url")]
        [InlineData("ftp://api.neckbook.com.br")]
        public void Validate_fails_when_BaseUrl_is_not_valid_absolute_http_uri(string rawValue)
        {
            var config = BuildConfiguration(rawValue);
            using var err = new StringWriter();

            var ok = NeckBookStartupValidator.Validate(config, err);

            Assert.False(ok);
            Assert.Contains("NeckBook:BaseUrl", err.ToString());
        }

        [Theory]
        [InlineData("https://api.neckbook.com.br")]
        [InlineData("https://api.neckbook.com.br/")]
        [InlineData("http://localhost:8000")]
        public void Validate_succeeds_when_BaseUrl_is_valid_absolute_uri(string validUrl)
        {
            var config = BuildConfiguration(validUrl);
            using var err = new StringWriter();

            var ok = NeckBookStartupValidator.Validate(config, err);

            Assert.True(ok);
            Assert.Equal(string.Empty, err.ToString());
        }
    }
}
