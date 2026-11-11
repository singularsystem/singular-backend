using System;

namespace SingularSystem.Services;

/// <summary>
/// Esqueleto HTML neutro de e-mails transacionais da Singular System.
/// Utiliza a marca oficial em texto e a paleta oficial (#111E42, #FF7623, #F5F7FB).
/// </summary>
public static class EmailLayout
{
    public const string BrandTitle = "Singular System";
    public const string BrandSubtitle = "Gestão Especializada em Atendimento Educacional Especializado (AEE)";
    public const string PrimaryColor = "#111E42";
    public const string AccentColor = "#FF7623";
    public const string BackgroundColor = "#F5F7FB";
    public const string TextColor = "#212529";

    /// <summary>
    /// Envelopa o conteúdo do e-mail com o cabeçalho e rodapé oficiais da marca.
    /// </summary>
    public static string Wrap(string title, string innerHtml)
    {
        return $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: {BackgroundColor}; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: {TextColor}; line-height: 1.6;"">
    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: {BackgroundColor}; padding: 24px 0;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""600"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.08);"">
                    <!-- Cabeçalho -->
                    <tr>
                        <td style=""background-color: {PrimaryColor}; padding: 24px 32px; text-align: left;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 22px; font-weight: 700; letter-spacing: -0.5px;"">{BrandTitle}</h1>
                            <p style=""margin: 4px 0 0 0; color: #e2e8f0; font-size: 13px;"">{BrandSubtitle}</p>
                        </td>
                    </tr>
                    <!-- Filete de destaque -->
                    <tr>
                        <td style=""background-color: {AccentColor}; height: 4px; line-height: 4px; font-size: 4px;"">&nbsp;</td>
                    </tr>
                    <!-- Conteúdo Principal -->
                    <tr>
                        <td style=""padding: 32px; font-size: 15px; color: {TextColor};"">
                            {innerHtml}
                        </td>
                    </tr>
                    <!-- Rodapé -->
                    <tr>
                        <td style=""background-color: {BackgroundColor}; padding: 20px 32px; border-top: 1px solid #e2e8f0; font-size: 12px; color: #64748b; text-align: center;"">
                            <p style=""margin: 0 0 4px 0;"">Este é um e-mail automático gerado pela plataforma {BrandTitle}.</p>
                            <p style=""margin: 0;"">© {DateTime.UtcNow.Year} {BrandTitle}. Todos os direitos reservados.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
