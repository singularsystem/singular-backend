using System;
using System.IO;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Fonts;
using Xunit;

namespace SingularSystem.Tests;

public sealed class PdfFontsPropertyTests
{
    private static readonly string RealFontsDir = Path.Combine(AppContext.BaseDirectory, "Fonts");

    public sealed record MissingFontTestCase(string MissingFontFile, string[] AvailableFontFiles);

    public static class MissingFontTestCaseGen
    {
        public static Arbitrary<MissingFontTestCase> Arbitrary()
        {
            var gen = Gen.Elements(PdfFonts.Required).Select(missing =>
            {
                var available = Array.FindAll(PdfFonts.Required, f => f != missing);
                return new MissingFontTestCase(missing, available);
            });

            return gen.ToArbitrary();
        }
    }

    // Feature: modulo-2-rebranding-pdfs, Property 6: Fonte obrigatória ausente falha com o nome do arquivo
    //
    // Para qualquer arquivo da lista PdfFonts.Required removido de um diretório de fontes de teste,
    // EnsureRegistered lança FileNotFoundException cuja mensagem contém o nome desse arquivo,
    // e não registra nenhuma fonte substituta.
    //
    // Validates: Requirements 6.4
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(MissingFontTestCaseGen) })]
    public Property Property_6_Missing_Required_Font_Throws_FileNotFoundException_With_File_Name(MissingFontTestCase testCase)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "fonts_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Copia todas as fontes EXCETO a fonte que deve faltar
            foreach (var available in testCase.AvailableFontFiles)
            {
                var src = Path.Combine(RealFontsDir, available);
                if (File.Exists(src))
                {
                    File.Copy(src, Path.Combine(tempDir, available), overwrite: true);
                }
            }

            // Invoca EnsureRegistered com o diretório incompleto
            var thrown = false;
            var mentionsFileName = false;

            try
            {
                PdfFonts.EnsureRegistered(tempDir);
            }
            catch (FileNotFoundException fnfEx)
            {
                thrown = true;
                // Deve nomear o arquivo ausente na mensagem ou na propriedade FileName
                mentionsFileName = (fnfEx.Message != null && fnfEx.Message.Contains(testCase.MissingFontFile))
                                   || string.Equals(fnfEx.FileName, testCase.MissingFontFile, StringComparison.OrdinalIgnoreCase);
            }

            return (thrown && mentionsFileName).ToProperty()
                .Label($"Expected FileNotFoundException naming '{testCase.MissingFontFile}', thrown={thrown}, mentions={mentionsFileName}");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }
    }

    [Fact]
    public void EnsureRegistered_With_All_Fonts_Succeeds_Idempotently()
    {
        // Executar com o diretório real completo não deve lançar exceção
        var ex = Record.Exception(() => PdfFonts.EnsureRegistered(RealFontsDir));
        Assert.Null(ex);

        // Chamada repetida (idempotente)
        var ex2 = Record.Exception(() => PdfFonts.EnsureRegistered(RealFontsDir));
        Assert.Null(ex2);
    }
}
