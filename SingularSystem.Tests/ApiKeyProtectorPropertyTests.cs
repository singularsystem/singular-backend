using System;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Security;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: neckbook-books-proxy, Property 2: Round-trip de proteção da chave de API
    // Validates: Requirements 4.3
    public sealed class ApiKeyProtectorPropertyTests
    {
        private static readonly ApiKeyProtector Protector =
            new(new EphemeralDataProtectionProvider());

        // Feature: neckbook-books-proxy, Property 2: Round-trip de proteção da chave de API
        [Property(MaxTest = 100)]
        public Property ApiKey_protect_roundtrip(NonEmptyString rawKey)
        {
            var key = rawKey.Get.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                key = "nk_live_default_key";
            }

            var encrypted = Protector.Protect(key);
            var decrypted = Protector.Unprotect(encrypted);

            return (decrypted == key && encrypted != key)
                .Label($"Expected round-trip match and non-plaintext cipher for key length {key.Length}");
        }
    }
}
