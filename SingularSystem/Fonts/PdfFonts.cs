using System;
using System.IO;
using QuestPDF.Drawing;

namespace SingularSystem.Fonts;

public static class PdfFonts
{
    public static readonly string[] Required =
    {
        "Poppins-Regular.ttf",
        "Poppins-Medium.ttf",
        "Poppins-SemiBold.ttf",
        "Poppins-Bold.ttf",
        "DMSans-Regular.ttf",
        "DMSans-Italic.ttf",
        "DMSans-Medium.ttf",
        "DMSans-SemiBold.ttf",
        "DMSans-Bold.ttf"
    };

    private static readonly object SyncLock = new();
    private static bool _isRegistered = false;

    /// <summary>
    /// Registra todas as fontes obrigatórias (Poppins e DM Sans) no QuestPDF FontManager.
    /// É idempotente e thread-safe. Lança FileNotFoundException identificando o arquivo ausente caso qualquer fonte não seja encontrada.
    /// </summary>
    /// <param name="fontsDirectory">Diretório alternativo para testes. Se nulo, utiliza AppContext.BaseDirectory/Fonts.</param>
    public static void EnsureRegistered(string? fontsDirectory = null)
    {
        var dir = fontsDirectory ?? Path.Combine(AppContext.BaseDirectory, "Fonts");

        // Verifica existência de cada fonte obrigatória sem fallback silencioso (Requisito 6.4)
        foreach (var fontFile in Required)
        {
            var fullPath = Path.Combine(dir, fontFile);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Arquivo de fonte obrigatório não encontrado: '{fontFile}' no diretório '{dir}'.", fontFile);
            }
        }

        lock (SyncLock)
        {
            if (fontsDirectory == null && _isRegistered)
            {
                return;
            }

            foreach (var fontFile in Required)
            {
                var fullPath = Path.Combine(dir, fontFile);
                using var stream = File.OpenRead(fullPath);
                FontManager.RegisterFont(stream);
            }

            if (fontsDirectory == null)
            {
                _isRegistered = true;
            }
        }
    }

    /// <summary>
    /// Reseta o estado de registro para testes isolados.
    /// </summary>
    public static void ResetRegistrationForTests()
    {
        lock (SyncLock)
        {
            _isRegistered = false;
        }
    }
}
