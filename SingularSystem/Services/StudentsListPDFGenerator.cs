using SingularSystem.DTOs.Students;
using SingularSystem.Extensions;
using SingularSystem.Interfaces;
using SingularSystem.Models;
using SingularSystem.Models.Enums;
using SingularSystem.Templates;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace SingularSystem.Services
{
    public class StudentsListPDFGenerator : IStudentPDFGenerator
    {
        public string Discriminator => "StudentsList";
        public Task<byte[]> GenerateStudentsPDFAsync(List<StudentsPDFResponse> rows, CancellationToken ct = default)
        {
            var bytes = new StudentPDFDocument(rows).GeneratePdf();
            return Task.FromResult(bytes);
        }

        public class StudentPDFDocument : IDocument
        {
            private readonly List<StudentsPDFResponse> _rows;
            private readonly PdfRenderContext _context;

            public StudentPDFDocument(List<StudentsPDFResponse> rows, PdfRenderContext? context = null)
            {
                _rows = rows ?? new();
                _context = context ?? PdfRenderContext.Default(PdfProfile.View);
            }

            public DocumentMetadata GetMetadata() => PdfMetadataFactory.CreateMetadata(_context.Profile, "Listagem de Alunos");
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
                        PdfBrandChrome.ComposeHeader(h, _context, "Listagem de Alunos");
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
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Nome");
                                    header.Cell().Element(HeaderCell).Text("Data de Nascimento");
                                    header.Cell().Element(HeaderCell).Text("Escola");
                                    header.Cell().Element(HeaderCell).Text("Série");
                                    header.Cell().Element(HeaderCell).Text("Município");
                                    header.Cell().Element(HeaderCell).Text("Estado");

                                    header.Cell()
                                          .ColumnSpan(6)
                                          .BorderBottom(2)
                                          .BorderColor(PdfTheme.Colors.Line)
                                          .PaddingTop(6);
                                });

                                for (int i = 0; i < _rows.Count; i++)
                                {
                                    var r = _rows[i];
                                    var isEven = i % 2 == 0;

                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.FullName ?? "-");
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(PdfTheme.FormatDate(r.DateOfBirth, culture));
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.SchoolName ?? "-");
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.Serie ?? "-");
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.City ?? "-");
                                    table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.State ?? "-");
                                }

                                table.Cell()
                                     .ColumnSpan(6)
                                     .BorderBottom(1)
                                     .BorderColor(PdfTheme.Colors.Line)
                                     .PaddingVertical(4);
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
                    .DefaultTextStyle(_ => PdfTheme.HeaderCellStyle)
                    .PaddingVertical(2);
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
