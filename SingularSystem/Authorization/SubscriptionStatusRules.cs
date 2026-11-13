using SingularSystem.Domain.Plans.Enums;

namespace SingularSystem.Authorization
{
    public static class SubscriptionStatusRules
    {
        // Escrita = POST/PUT/PATCH/DELETE (Req 10.1). GET/HEAD/OPTIONS são leitura.
        public static bool IsWrite(string httpMethod) =>
            httpMethod is "POST" or "PUT" or "PATCH" or "DELETE";

        /// <summary>
        /// Retorna null quando permitido; caso contrário, a mensagem de bloqueio (HTTP 403).
        /// </summary>
        public static string? Evaluate(SubscriptionStatus status, string httpMethod) => status switch
        {
            SubscriptionStatus.Suspended => "Assinatura suspensa. Regularize o pagamento para continuar.",
            SubscriptionStatus.Cancelled => "Assinatura cancelada.",
            SubscriptionStatus.PastDue when IsWrite(httpMethod)
                => "Assinatura vencida. Apenas consulta é permitida até a regularização.",
            _ => null
        };
    }
}
