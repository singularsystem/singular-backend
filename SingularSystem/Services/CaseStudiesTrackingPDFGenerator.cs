using SingularSystem.DTOs.Tracking;
using SingularSystem.Templates;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SingularSystem.Services
{
    public class CaseStudiesTrackingPDFGenerator
    {
        public static byte[] Generate(List<CaseStudiesTrackingPDFRow> rows)
        {
            var document = new BaseA4Document("Acompanhamento de Estudos de Caso", container =>
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
                            columns.RelativeColumn(2); // Status
                            columns.RelativeColumn(2); // Última Atualização
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Aluno");
                            header.Cell().Element(HeaderCell).Text("Escola");
                            header.Cell().Element(HeaderCell).Text("Status");
                            header.Cell().Element(HeaderCell).Text("Última Atualização");

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
                            table.Cell().Element(c => BodyCell(c, isEven)).ShowEntire().Text(r.LastUpdate ?? "-");
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
