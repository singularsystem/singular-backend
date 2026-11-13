namespace SingularSystem.Models
{
    public enum DocumentPdfOutcome { Success, NotFound, Forbidden }

    public sealed record DocumentPdfResult(DocumentPdfOutcome Outcome, byte[]? Bytes = null, string? DocumentType = null)
    {
        public static DocumentPdfResult Ok(byte[] bytes, string? type) => new(DocumentPdfOutcome.Success, bytes, type);
        public static DocumentPdfResult Missing() => new(DocumentPdfOutcome.NotFound);
        public static DocumentPdfResult Denied() => new(DocumentPdfOutcome.Forbidden);
    }
}
