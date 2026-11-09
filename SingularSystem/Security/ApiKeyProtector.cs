using Microsoft.AspNetCore.DataProtection;

namespace SingularSystem.Security
{
    /// <summary>
    /// Serviço responsável por criptografar e descriptografar a <c>NeckBook_Api_Key</c>
    /// em repouso via ASP.NET Core Data Protection (<see cref="IDataProtectionProvider"/>)
    /// com propósito dedicado <c>"NeckBook.ApiKey.v1"</c> (Req 10.6, 10.7).
    /// </summary>
    public sealed class ApiKeyProtector
    {
        public const string Purpose = "NeckBook.ApiKey.v1";

        private readonly IDataProtector _protector;

        public ApiKeyProtector(IDataProtectionProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            _protector = provider.CreateProtector(Purpose);
        }

        public string Protect(string plaintextKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(plaintextKey);
            return _protector.Protect(plaintextKey);
        }

        public string Unprotect(string encryptedKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(encryptedKey);
            return _protector.Unprotect(encryptedKey);
        }
    }
}
