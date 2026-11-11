using System;
using System.Text.RegularExpressions;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.Rendering;

namespace SingularSystem.Services;

public static class QrCodeSvg
{
    private static readonly Regex ScriptTagRegex = new(@"<script[\s\S]*?>[\s\S]*?<\/script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EventAttributeRegex = new(@"\son\w+\s*=\s*(['""]).*?\1", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex XmlDeclRegex = new(@"<\?xml[\s\S]*?\?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DoctypeRegex = new(@"<!DOCTYPE[\s\S]*?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Renderiza um QR Code em formato vetorial SVG limpo e seguro (correção de erro M, margem de 4 módulos).
    /// </summary>
    /// <param name="payload">Conteúdo/URL a ser codificado.</param>
    /// <param name="sizePx">Dimensões em pixels para o SVG.</param>
    /// <returns>String SVG sanitizada com viewBox e namespaces seguros.</returns>
    public static string Render(string payload, int sizePx = 120)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload do QR Code não pode ser nulo ou vazio.", nameof(payload));
        }

        var effectiveSize = sizePx > 0 ? sizePx : 120;

        var writer = new BarcodeWriterSvg
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                ErrorCorrection = ErrorCorrectionLevel.M,
                Margin = 4,
                Width = effectiveSize,
                Height = effectiveSize,
                CharacterSet = "UTF-8"
            },
            Renderer = new SvgRenderer()
        };

        var svgResult = writer.Write(payload);
        var rawContent = svgResult.Content;

        return CleanSvg(rawContent, effectiveSize);
    }

    /// <summary>
    /// Sanitiza o SVG removendo scripts, manipuladores de evento, declarações XML/DOCTYPE e assegurando atributos seguros.
    /// </summary>
    public static string CleanSvg(string rawSvg, int sizePx)
    {
        if (string.IsNullOrWhiteSpace(rawSvg))
        {
            return string.Empty;
        }

        var cleaned = XmlDeclRegex.Replace(rawSvg, string.Empty);
        cleaned = DoctypeRegex.Replace(cleaned, string.Empty);
        cleaned = ScriptTagRegex.Replace(cleaned, string.Empty);
        cleaned = EventAttributeRegex.Replace(cleaned, string.Empty);
        cleaned = cleaned.Trim();

        // Se não possui viewBox mas tem svg, insere viewBox padronizado
        if (cleaned.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) &&
            !cleaned.Contains("viewBox", StringComparison.OrdinalIgnoreCase))
        {
            var insertPos = cleaned.IndexOf('>');
            if (insertPos > 0)
            {
                var tagStart = cleaned.Substring(0, insertPos);
                var tagRest = cleaned.Substring(insertPos);
                cleaned = $"{tagStart} viewBox=\"0 0 {sizePx} {sizePx}\"{tagRest}";
            }
        }

        return cleaned;
    }
}
