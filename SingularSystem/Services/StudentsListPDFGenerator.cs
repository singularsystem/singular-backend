using SingularSystem.DTOs.Students;
using SingularSystem.Extensions;
using SingularSystem.Interfaces;
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

            public StudentPDFDocument(List<StudentsPDFResponse> rows)
            {
                _rows = rows ?? new();
            }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public void Compose(IDocumentContainer container)
            {
                var culture = new CultureInfo("pt-BR");

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(_ => PdfTheme.BaseStyle);

                    page.Header().ShowOnce().Element(h =>
                    {
                        h.Column(col =>
                        {
                            col.Item()
                                .AlignCenter()
                                .Text("IPAAGS · Sistema de Gestão Educacional")
                                .Style(PdfTheme.MutedStyle);

                            col.Item().PaddingTop(4);

                            col.Item()
                                .AlignCenter()
                                .Text("Listagem de Alunos")
                                .Style(PdfTheme.TitleStyle);

                            col.Item().PaddingTop(48);
                            col.Item().Element(x => PdfTheme.Hr(x, 2));
                        });
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

                    page.Footer().Element(f =>
                    {
                        f.PaddingTop(6).Column(col =>
                        {
                            col.Item().PaddingBottom(6).Element(x => PdfTheme.Hr(x, 2));

                            col.Item()
                                .AlignCenter()
                                .Text($"Documento gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm:ss}")
                                .Style(PdfTheme.MutedStyle);

                            col.Item()
                                .AlignCenter()
                                .Text("Sistema AEE - IPAAGS")
                                .Style(PdfTheme.MutedStyle);

                            col.Item().AlignCenter()
                               .Text(t =>
                               {
                                   t.Span("Página ").Style(PdfTheme.MutedStyle);
                                   t.CurrentPageNumber().Style(PdfTheme.MutedStyle);
                                   t.Span(" de ").Style(PdfTheme.MutedStyle);
                                   t.TotalPages().Style(PdfTheme.MutedStyle);
                               });
                        });
                    });
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
