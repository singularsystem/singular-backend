using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SingularSystem.Configuration;
using SingularSystem.Interfaces;

namespace SingularSystem.Services;

/// <summary>
/// Implementação de <see cref="IEmailService"/> via SMTP simples (Req 6.1, 6.2, 6.3).
/// Lê configurações de SmtpOptions parametrizadas por variáveis de ambiente.
/// Conteúdo é gerado nativamente neutro sem necessidade de sanitização em runtime.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("O destinatário do e-mail é obrigatório.", nameof(to));

        try
        {
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.Port != 25,
                Credentials = new NetworkCredential(_options.Username, _options.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, "Singular System"),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
            _logger.LogInformation("E-mail enviado com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no envio de e-mail.");
            throw;
        }
    }
}
