namespace SingularSystem.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime ToBrazilianTime(this DateTime dateTime)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows()
                    ? "E. South America Standard Time"
                    : "America/Sao_Paulo");

            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZone);
        }

        public static DateTime? ToBrazilianTime(this DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return null;

            return dateTime.Value.ToBrazilianTime();
        }
    }
}
