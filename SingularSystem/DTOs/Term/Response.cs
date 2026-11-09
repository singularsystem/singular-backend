namespace SingularSystem.DTOs.Term
{
    public record TermResponse(
        Guid Id,
        string Key,
        string Version,
        string Title,
        string Content,
        string ContentFormat,
        DateTime PublishedAt,
        string ContentHash
    );

    public record ActiveTermStatusResponse(
        bool HasAccepted,
        DateTime? AcceptedAt,
        string? TermVersion,
        Guid? TermId
    );
}
