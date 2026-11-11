using SingularSystem.DTOs.ReportAEE;
using SingularSystem.DTOs.Students;
using SingularSystem.Extensions;
using SingularSystem.Interfaces;
using SingularSystem.Models;
using SingularSystem.Templates;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace SingularSystem.Services
{
    public class ReportsAEEPDFGenerator : IReportAEEPDFGenerator
    {
        public string Discriminator => "ReportAEE";
        public Task<byte[]> GenerateReportsAEEPDFAsync(List<ReportAEEPDFResponse> rows, CancellationToken ct = default)
        {
            var bytes = new ReportAEEPDFDocument(rows).GeneratePdf();
            return Task.FromResult(bytes);
        }

        public class ReportAEEPDFDocument : IDocument
        {
            private readonly List<ReportAEEPDFResponse> _rows;
            private readonly PdfRenderContext _context;

            public ReportAEEPDFDocument(List<ReportAEEPDFResponse> rows, PdfRenderContext? context = null)
            {
                _rows = rows ?? new();
                _context = context ?? PdfRenderContext.Default(PdfProfile.View);
            }

            public DocumentMetadata GetMetadata() => PdfMetadataFactory.CreateMetadata(_context.Profile, "Relatório Geral AEE");
            public DocumentSettings GetSettings() => PdfMetadataFactory.CreateSettings(_context.Profile);

            public void Compose(IDocumentContainer container)
            {
                var culture = new CultureInfo("pt-BR");

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(_ => PdfTheme.BaseStyle);

                    page.Header().Element(h =>
                    {
                        PdfBrandChrome.ComposeHeader(h, _context, "Relatório Geral AEE");
                    });

                    page.Content()
                        .PaddingTop(8)
                        .PaddingBottom(16)
                        .Column(x =>
                        {
                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Nº");
                                    header.Cell().Element(HeaderCell).Text("Nome");
                                    header.Cell().Element(HeaderCell).Text("Idade");
                                    header.Cell().Element(HeaderCell).Text("Deficiências");
                                    header.Cell().Element(HeaderCell).Text("Transtornos");
                                    header.Cell().Element(HeaderCell).Text("Estado");
                                    header.Cell().Element(HeaderCell).Text("Município");
                                    header.Cell().Element(HeaderCell).Text("Escola");
                                    header.Cell().Element(HeaderCell).Text("Série");

                                    header.Cell()
                                          .ColumnSpan(9)
                                          .BorderBottom(2)
                                          .BorderColor(PdfTheme.Colors.Line)
                                          .PaddingTop(6);
                                });

                                for (int i = 0; i < _rows.Count; i++)
                                {
                                    var r = _rows[i];
                                    var isEven = i % 2 == 0;

                                    var disabilitiesText =
                                        r.Disabilities != null && r.Disabilities.Any()
                                            ? string.Join(", ", r.Disabilities)
                                            : "-";

                                    var disordersText =
                                        r.Disorders != null && r.Disorders.Any()
                                            ? string.Join(", ", r.Disorders)
                                            : "-";

                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text((i + 1).ToString());
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.FullName ?? "-");
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.Age.ToString() ?? "-");
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(disabilitiesText);
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(disordersText);
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.State ?? "-");
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.City ?? "-");
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.SchoolName ?? "-");
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.Serie ?? "-");
                                }

                                table.Cell()
                                     .ColumnSpan(9)
                                     .BorderBottom(1)
                                     .BorderColor(PdfTheme.Colors.Line);
                            });
                        });

                    page.Footer().Element(f => PdfBrandChrome.ComposeFooter(f, _context));
                });
            }

            private static IContainer HeaderCell(IContainer c)
            {
                return c
                    .AlignCenter()
                    .AlignMiddle()
                    .DefaultTextStyle(_ => PdfTheme.HeaderCellStyle);
                    //.PaddingVertical(2);
            }

            private static IContainer BodyCell(IContainer c, bool isEvenRow)
            {
                return c
                    .AlignCenter()
                    .AlignMiddle()
                    .DefaultTextStyle(_ => PdfTheme.BodyCellStyle)
                    .CellPadding();
            }
        }
    }
}
