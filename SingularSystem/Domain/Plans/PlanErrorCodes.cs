namespace SingularSystem.Domain.Plans
{
    public static class PlanErrorCodes
    {
        public const string QuotaExceeded = "PLAN_QUOTA_EXCEEDED";
        public const string UserLimitExceeded = "USER_LIMIT_EXCEEDED";
        public const string FeatureUnavailable = "PLAN_FEATURE_UNAVAILABLE";
        public const string SubscriptionPastDue = "SUBSCRIPTION_PAST_DUE";
        public const string SubscriptionSuspended = "SUBSCRIPTION_SUSPENDED";
        public const string SubscriptionCancelled = "SUBSCRIPTION_CANCELLED";
        public const string ProfessionalTypeDisabled = "PROFESSIONAL_TYPE_DISABLED";
    }

    public record QuotaInfo(int Current, int Max);
    public record SubscriptionDenial(string Message, string ErrorCode);
}
