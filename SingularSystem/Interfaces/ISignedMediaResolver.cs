namespace SingularSystem.Interfaces
{
    /// <summary>
    /// Abstração reutilizável das três operações de resolução de mídia assinada extraídas de
    /// <c>LegacyMediaMigrationService</c> (spec 012): a detecção de origem de um <c>SignedFile</c>
    /// (ZapSign legado vs. S3 próprio do Singular System) e a geração de URL pré-assinada do S3 próprio.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Permite que o novo endpoint agnóstico (<c>DocumentsSignatureController</c>) consuma a lógica
    /// de detecção/presign sem depender da rotina de backup/migração inteira
    /// (<c>LegacyMediaMigrationService</c>), respeitando SRP e evitando duplicação de lógica (DRY).
    /// </para>
    /// <para>
    /// As implementações devem preservar o comportamento atual: correspondência de host contra os
    /// domínios do ZapSign em <see cref="IsZapSignUrl"/>, inspeção de host do S3 próprio/chave
    /// relativa em <see cref="IsOwnS3Reference"/>, e o clamp de TTL a no máximo 60 minutos em
    /// <see cref="GeneratePreSignedUrl"/>.
    /// </para>
    /// </remarks>
    public interface ISignedMediaResolver
    {
        /// <summary>
        /// Determina se o valor de <c>SignedFile</c> aponta para o domínio do ZapSign
        /// (documento legado ainda não migrado).
        /// </summary>
        /// <param name="signedFile">Valor atual do campo <c>SignedFile</c> da entidade.</param>
        /// <returns>
        /// <c>true</c> somente quando <paramref name="signedFile"/> for uma URL absoluta cujo host
        /// pertença ao domínio do ZapSign; caso contrário, <c>false</c>.
        /// </returns>
        bool IsZapSignUrl(string? signedFile);

        /// <summary>
        /// Determina se o valor de <c>SignedFile</c> já se refere ao S3 próprio do Singular System — seja uma
        /// chave de objeto relativa, seja uma URL absoluta cujo host pertença ao bucket/endpoint S3
        /// configurado.
        /// </summary>
        /// <param name="signedFile">Valor atual do campo <c>SignedFile</c> da entidade.</param>
        /// <returns><c>true</c> quando aponta para o S3 próprio; caso contrário, <c>false</c>.</returns>
        bool IsOwnS3Reference(string? signedFile);

        /// <summary>
        /// Gera uma URL pré-assinada do S3 próprio para acesso temporário ao PDF migrado a partir da
        /// chave do objeto.
        /// </summary>
        /// <param name="s3ObjectKey">Chave do objeto no bucket do S3 próprio.</param>
        /// <param name="durationMinutes">TTL desejado em minutos; limitado a no máximo 60 (1h).</param>
        /// <returns>A URL pré-assinada para acesso temporário ao PDF migrado.</returns>
        string GeneratePreSignedUrl(string s3ObjectKey, double durationMinutes = 60);
    }
}
