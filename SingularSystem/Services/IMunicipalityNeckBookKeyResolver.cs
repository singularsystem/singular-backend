namespace SingularSystem.Services
{
    /// <summary>
    /// Resolve em memória a <c>NeckBook_Api_Key</c> em texto claro associada ao município
    /// <c>(State, City)</c> do usuário autenticado, ou <c>null</c> quando o município não está
    /// cadastrado ou está inativo (<c>IsActive == false</c>) (Req 5.1–5.6).
    /// </summary>
    public interface IMunicipalityNeckBookKeyResolver
    {
        Task<string?> ResolveApiKeyAsync(string? state, string? city, CancellationToken ct = default);
    }
}
