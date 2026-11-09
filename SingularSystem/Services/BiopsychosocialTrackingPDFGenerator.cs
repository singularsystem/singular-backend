using SingularSystem.DTOs.Tracking;
using SingularSystem.Templates;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SingularSystem.Services
{
    public class BiopsychosocialTrackingPDFGenerator
    {
        public static byte[] Generate(List<BiopsychosocialTrackingPDFRow> rows)
        {
            var document = new BaseA4Document("Acompanhamento Biopsicossocial", container =>
            {
                container.Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Aluno
                            columns.RelativeColumn(3); // Escola
                            columns.RelativeColumn(2); // Status Geral
                            columns.RelativeColumn(4); // Profissionais Pendentes
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Aluno");
                            header.Cell().Element(HeaderCell).Text("Escola");
                            header.Cell().Element(HeaderCell).Text("Status Geral");
                            header.Cell().Element(HeaderCell).Text("Profissionais Pendentes");

                            header.Cell()
                                  .ColumnSpan(4)
                                  .BorderBottom(2)
                                  .BorderColor(PdfTheme.Colors.Line)
                                  .PaddingTop(6);
                        });

                        for (int i = 0; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var isEven = i % 2 == 0;

                            table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.StudentName ?? "-");
                            table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.SchoolName ?? "-");
                            table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.Status ?? "-");
                            table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.PendingProfessionals ?? "-");
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static IContainer HeaderCell(IContainer c)
        {
            return c
                .AlignLeft()
                .AlignMiddle()
                .DefaultTextStyle(_ => PdfTheme.HeaderCellStyle)
                .PaddingVertical(4);
        }

        private static IContainer BodyCell(IContainer c, bool isEvenRow)
        {
            var container = c
                .AlignLeft()
                .AlignMiddle()
                .DefaultTextStyle(_ => PdfTheme.BodyCellStyle)
                .CellPadding();

            if (isEvenRow)
            {
                container = container.Background(Color.FromHex("#F9FAFB"));
            }

            return container;
        }
    }
}
