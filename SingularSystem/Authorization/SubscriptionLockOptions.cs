namespace SingularSystem.Authorization
{
    public static class SubscriptionLockOptions
    {
        // Templates de rota (AttributeRouteInfo.Template, normalizados em minúsculas, sem barra final).
        // Limitado a autenticação e users/me.
        public static readonly IReadOnlySet<string> ExemptRouteTemplates =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "api/v1/users/login",
                "api/v1/users/logout",
                "api/v1/users/me"
            };
    }
}
