using System;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SingularSystem.Models;
using SingularSystem.Services;

namespace SingularSystem.Templates;

public static class PdfBrandChrome
{
    public const string BrandFooterText = "Singular System - Gestão Especializada em AEE";

    /// <summary>
    /// Cabeçalho da primeira página: logotipo + dados do emissor + título do documento.
    /// </summary>
    public static void ComposeHeader(IContainer c, PdfRenderContext ctx, string documentTitle)
    {
        c.Column(col =>
        {
            col.Item().Row(r =>
            {
                // Esquerda: dados do emissor
                r.RelativeItem().Column(issuerCol =>
                {
                    if (ctx.Issuer.Lines.Count > 0)
                    {
                        issuerCol.Item().Text(ctx.Issuer.DisplayName).Style(PdfTheme.H2);

                        for (int i = 1; i < ctx.Issuer.Lines.Count; i++)
                        {
                            issuerCol.Item().Text(ctx.Issuer.Lines[i]).Style(PdfTheme.MutedStyle);
                        }
                    }
                    else
                    {
                        issuerCol.Item().Text(IssuerHeaderModelFactory.DefaultBrandName).Style(PdfTheme.H2);
                    }
                });

                // Direita: logotipo
                var logoBytes = (ctx.Issuer.LogoBytes != null && ctx.Issuer.LogoBytes.Length > 0)
                    ? ctx.Issuer.LogoBytes
                    : ctx.SingularLogo;

                if (logoBytes != null && logoBytes.Length > 0)
                {
                    var validation = LogoValidator.Validate(logoBytes, maxBytes: 10 * 1024 * 1024, minWidthPx: 1);
                    float wMm = LogoSizing.DefaultMaxWidthMm;
                    float hMm = LogoSizing.DefaultMaxHeightMm;
                    if (validation.IsValid && validation.Logo != null)
                    {
                        var size = LogoSizing.ComputeDisplaySize(validation.Logo.WidthPx, validation.Logo.HeightPx);
                        if (size.WidthMm > 0 && size.HeightMm > 0)
                        {
                            wMm = size.WidthMm;
                            hMm = size.HeightMm;
                        }
                    }

                    r.ConstantItem(wMm, Unit.Millimetre)
                        .Height(hMm, Unit.Millimetre)
                        .AlignRight()
                        .Image(logoBytes)
                        .FitArea();
                }
            });

            if (!string.IsNullOrWhiteSpace(documentTitle))
            {
                col.Item().PaddingTop(10).AlignCenter().Text(documentTitle).Style(PdfTheme.H1);
            }

            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(PdfTheme.BorderColor);
        });
    }

    /// <summary>
    /// Rodapé de todas as páginas: filete, "Documento gerado em dd/MM/yyyy às HH:mm", marca e "Página X de Y".
    /// </summary>
    public static void ComposeFooter(IContainer c, PdfRenderContext ctx)
    {
        c.PaddingTop(8).Column(col =>
        {
            col.Spacing(3);

            col.Item().LineHorizontal(1).LineColor(PdfTheme.BorderColor);

            var brt = ctx.GeneratedAtBrt;
            var ptBr = new CultureInfo("pt-BR");
            col.Item().AlignCenter().Text(text =>
            {
                text.Span("Documento gerado em ").Style(PdfTheme.MutedStyle);
                text.Span(brt.ToString("dd/MM/yyyy", ptBr)).Style(PdfTheme.MutedStyle);
                text.Span(" às ").Style(PdfTheme.MutedStyle);
                text.Span(brt.ToString("HH:mm", ptBr)).Style(PdfTheme.MutedStyle);
            });

            col.Item().AlignCenter().Text(BrandFooterText).Style(PdfTheme.MutedStyle);

            col.Item().AlignCenter().Text(t =>
            {
                t.Span("Página ").Style(PdfTheme.MutedStyle);
                t.CurrentPageNumber().Style(PdfTheme.MutedStyle);
                t.Span(" de ").Style(PdfTheme.MutedStyle);
                t.TotalPages().Style(PdfTheme.MutedStyle);
            });
        });
    }
}
