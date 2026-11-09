namespace SingularSystem.Configuration
{
    /// <summary>
    /// Opções de configuração para o serviço de e-mail transacional SMTP (Req 6.1, 6.2, ADR-004).
    /// </summary>
    public sealed class SmtpOptions
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587; // Válido: 1..65535
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
    }
}
