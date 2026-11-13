namespace SingularSystem.Domain.Plans
{
    public record QuotaResult(bool Allowed, int Current, int Max);
}
