using SingularSystem.Extensions;
using System.Globalization;

namespace SingularSystem.DTOs.Users
{
    public class LoginResponse
    {
        public string ID { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime? TokenExpiration { get; set; }
        public bool MustChangePassword { get; set; }
        public string LocalTokenExpiration => TokenExpiration?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? "";
    }
}
