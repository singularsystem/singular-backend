using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Plans.Enums;

namespace SingularSystem.Authorization
{
    public static class SubscriptionStatusRules
    {
        // Escrita = POST/PUT/PATCH/DELETE (Req 10.1). GET/HEAD/OPTIONS são leitura.
        public static bool IsWrite(string httpMethod) =>
            httpMethod is "POST" or "PUT" or "PATCH" or "DELETE";

        /// <summary>
        /// Retorna null quando permitido; caso contrário, detalhes de negação com mensagem e código de erro (HTTP 403).
        /// </summary>
        public static SubscriptionDenial? EvaluateDetailed(SubscriptionStatus status, string httpMethod) => status switch
        {
            SubscriptionStatus.Suspended => new SubscriptionDenial(
                "Assinatura suspensa. Regularize o pagamento para continuar.",
                PlanErrorCodes.SubscriptionSuspended),
            SubscriptionStatus.Cancelled => new SubscriptionDenial(
                "Assinatura cancelada.",
                PlanErrorCodes.SubscriptionCancelled),
            SubscriptionStatus.PastDue when IsWrite(httpMethod) => new SubscriptionDenial(
                "Assinatura vencida. Apenas consulta é permitida até a regularização.",
                PlanErrorCodes.SubscriptionPastDue),
            _ => null
        };

        /// <summary>
        /// Retorna null quando permitido; caso contrário, a mensagem de bloqueio (HTTP 403).
        /// </summary>
        public static string? Evaluate(SubscriptionStatus status, string httpMethod) =>
            EvaluateDetailed(status, httpMethod)?.Message;
    }
}
