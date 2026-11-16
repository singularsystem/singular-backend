using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Domain.Plans;
using SingularSystem.Models;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-3-frontend-gating, Property 1: ApiResponse omite errorCode nulo e preserva o código informado
    // Validates: Requirements 1.1, 1.2, 1.11
    public sealed class ApiResponsePropertyTests
    {
        private static readonly JsonSerializerOptions CamelCaseOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [Property(MaxTest = 100)]
        public Property ApiResponse_omits_null_error_code_and_preserves_provided_code(
            bool success,
            string? message,
            int total,
            bool hasErrorCode,
            PositiveInt codeIdx)
        {
            var validCodes = new[]
            {
                PlanErrorCodes.QuotaExceeded,
                PlanErrorCodes.UserLimitExceeded,
                PlanErrorCodes.FeatureUnavailable,
                PlanErrorCodes.SubscriptionPastDue,
                PlanErrorCodes.SubscriptionSuspended,
                PlanErrorCodes.SubscriptionCancelled,
                PlanErrorCodes.ProfessionalTypeDisabled
            };

            string? errorCode = hasErrorCode ? validCodes[codeIdx.Get % validCodes.Length] : null;

            var response = new ApiResponse<object>
            {
                Success = success,
                Message = message ?? string.Empty,
                Total = total,
                ErrorCode = errorCode
            };

            var json = JsonSerializer.Serialize(response, CamelCaseOptions);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Req 1.11: propriedades existentes preservadas
            var preservesSuccess = root.GetProperty("success").GetBoolean() == success;
            var preservesMessage = root.GetProperty("message").GetString() == (message ?? string.Empty);
            var preservesTotal = root.GetProperty("total").GetInt32() == total;

            // Req 1.1, 1.2: errorCode omitido quando nulo, preservado quando informado
            if (errorCode == null)
            {
                var omitted = !root.TryGetProperty("errorCode", out _) && !root.TryGetProperty("ErrorCode", out _);
                return (preservesSuccess && preservesMessage && preservesTotal && omitted).ToProperty();
            }
            else
            {
                var present = root.TryGetProperty("errorCode", out var prop) && prop.GetString() == errorCode;
                return (preservesSuccess && preservesMessage && preservesTotal && present).ToProperty();
            }
        }
    }
}
