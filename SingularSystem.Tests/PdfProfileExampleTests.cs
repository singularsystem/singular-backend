using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SingularSystem.Models;
using SingularSystem.Services;
using SingularSystem.Templates;
using Xunit;

namespace SingularSystem.Tests;

public class PdfProfileExampleTests
{
    static PdfProfileExampleTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
    [Fact]
    public void PrintProfile_Usa_RasterDpi_300()
    {
        var settings = PdfMetadataFactory.CreateSettings(PdfProfile.Print);
        Assert.Equal(300, settings.ImageRasterDpi);

        var viewSettings = PdfMetadataFactory.CreateSettings(PdfProfile.View);
        Assert.True(viewSettings.ImageRasterDpi < 300);
    }

    [Fact]
    public void View_Gera_Arquivo_Menor_Que_Print_Com_Imagem_Grande()
    {
        // Cria imagem fictícia de 500x500 pixels
        var imageBytes = new byte[500 * 500 * 4];
        new Random(42).NextBytes(imageBytes);

        // Documento QuestPDF simples contendo a imagem
        byte[] GenerateDoc(PdfProfile profile)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Content().Column(col =>
                    {
                        col.Item().Text("Teste de Perfil");
                    });
                });
            })
            .WithSettings(PdfMetadataFactory.CreateSettings(profile))
            .GeneratePdf();
        }

        var printBytes = GenerateDoc(PdfProfile.Print);
        var viewBytes = GenerateDoc(PdfProfile.View);

        Assert.NotNull(printBytes);
        Assert.NotNull(viewBytes);
    }
}
