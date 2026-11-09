using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace SingularSystem.Models
{
    public class Helpers
    {
        public static string NormalizeCity(string? city)
        {
            if (string.IsNullOrWhiteSpace(city)) return string.Empty;
            if (city.Equals("Todos", StringComparison.OrdinalIgnoreCase)) return "Todos";

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(city.Trim().ToLower());
        }

        public static string NormalizeState(string? state)
        {
            if (string.IsNullOrWhiteSpace(state)) return string.Empty;
            var fullState = GetFullStateName(state);
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fullState.Trim().ToLower());
        }

        public static string GetBrazilTimeZoneId()
        {
            return OperatingSystem.IsWindows()
                ? "E. South America Standard Time"
                : "America/Sao_Paulo";
        }

        public DateTime GetBrazilianCreatedAt(DateTime CreatedAt)
        {
            var brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById(GetBrazilTimeZoneId());
            return TimeZoneInfo.ConvertTimeFromUtc(CreatedAt, brazilTimeZone);
        }

        public static string? GetUserClaim(string token, IConfiguration configuration)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSecret = configuration["JWTSecret"];
            var signInKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret!)
            );

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signInKey,
                ValidateIssuer = false,
                ValidateAudience = false
            };

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

            var userIdClaim = principal.FindFirst("UserId")?.Value;

            return userIdClaim;
        }

        public static T GetEnumValueFromDisplayName<T>(string displayName) where T : Enum
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                // Tenta retornar o valor 'None' se ele existir no enum
                if (Enum.TryParse(typeof(T), "None", true, out var noneValue))
                {
                    return (T)noneValue;
                }
                return default!;
            }

            foreach (var field in typeof(T).GetFields())
            {
                var displayAttribute = field.GetCustomAttribute<DisplayAttribute>();

                if (displayAttribute?.Name != null && string.Equals(displayAttribute.Name, displayName, StringComparison.OrdinalIgnoreCase))
                {
                    return (T)field.GetValue(null)!;
                }
            }

            // Se não encontrar pelo DisplayName, tenta pelo nome do próprio campo (case-insensitive)
            if (Enum.TryParse(typeof(T), displayName, true, out var result))
            {
                return (T)result;
            }

            // Tenta retornar o valor 'None' se ele existir no enum antes de lançar erro
            if (Enum.TryParse(typeof(T), "None", true, out var fallbackNone))
            {
                return (T)fallbackNone;
            }

            throw new ArgumentException($"Nenhum valor do enum {typeof(T).Name} corresponde ao nome de exibição '{displayName}'");
        }

        public static string GetFullStateName(string state)
        {
            if (string.IsNullOrWhiteSpace(state)) return string.Empty;
            var s = state.Trim().ToUpperInvariant();
            return s switch
            {
                "AC" => "Acre",
                "AL" => "Alagoas",
                "AP" => "Amapá",
                "AM" => "Amazonas",
                "BA" => "Bahia",
                "CE" => "Ceará",
                "DF" => "Distrito Federal",
                "ES" => "Espírito Santo",
                "GO" => "Goiás",
                "MA" => "Maranhão",
                "MT" => "Mato Grosso",
                "MS" => "Mato Grosso do Sul",
                "MG" => "Minas Gerais",
                "PA" => "Pará",
                "PB" => "Paraíba",
                "PR" => "Paraná",
                "PE" => "Pernambuco",
                "PI" => "Piauí",
                "RJ" => "Rio de Janeiro",
                "RN" => "Rio Grande do Norte",
                "RS" => "Rio Grande do Sul",
                "RO" => "Rondônia",
                "RR" => "Roraima",
                "SC" => "Santa Catarina",
                "SP" => "São Paulo",
                "SE" => "Sergipe",
                "TO" => "Tocantins",
                _ => state
            };
        }

        public static bool StateMatches(string? state1, string? state2)
        {
            if (string.IsNullOrWhiteSpace(state1) && string.IsNullOrWhiteSpace(state2)) return true;
            if (string.IsNullOrWhiteSpace(state1) || string.IsNullOrWhiteSpace(state2)) return false;

            var fullState1 = GetFullStateName(state1);
            var fullState2 = GetFullStateName(state2);

            return string.Equals(fullState1, fullState2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool CityMatches(string? city1, string? city2)
        {
            if (string.IsNullOrWhiteSpace(city1) && string.IsNullOrWhiteSpace(city2)) return true;
            if (string.IsNullOrWhiteSpace(city1) || string.IsNullOrWhiteSpace(city2)) return false;

            return string.Equals(city1.Trim(), city2.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public int Total { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int UniqueTotal { get; set; }
    }

    public class MCHATResponse
    {
        public int Question { get; set; }
        public string Answer { get; set; }
    }

    public class QuestionnairesResponses
    {
        public List<MCHATResponse> MCHATR_Responses { get; set; }
        public List<CARSResponses> CARS_Responses { get; set; }
        public Dictionary<int, string> SNAP_IV_Responses { get; set; }
    }

    public class CARSResponses
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public string Label { get; set; }
    }

    public static class Hashing
    {
        public static string Sha256Hex(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
