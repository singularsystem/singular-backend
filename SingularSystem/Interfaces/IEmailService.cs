using System.Threading.Tasks;

namespace SingularSystem.Interfaces
{
    /// <summary>
    /// Abstração de envio transacional de e-mails da Singular System (Req 6.1, ADR-004).
    /// </summary>
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    }
}
