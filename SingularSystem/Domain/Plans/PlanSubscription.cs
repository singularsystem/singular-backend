using SingularSystem.Domain.Plans.Enums;

namespace SingularSystem.Domain.Plans
{
    public class PlanSubscription
    {
        public Guid ID { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public PlanType PlanType { get; set; }
        public SubscriptionStatus Status { get; set; }
        public int QuotaDocuments { get; set; }  // 0 = ilimitado
        public int MaxUsers { get; set; }         // 0 = ilimitado
        public string? ExternalPaymentId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
