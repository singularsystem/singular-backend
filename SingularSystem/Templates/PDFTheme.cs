using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace SingularSystem.Templates
{
    public static class PdfTheme
    {
        public static void RegisterFonts()
        {
            FontManager.RegisterFont(File.OpenRead("Fonts/Poppins-Regular.ttf"));
            FontManager.RegisterFont(File.OpenRead("Fonts/Poppins-Medium.ttf"));
            FontManager.RegisterFont(File.OpenRead("Fonts/Poppins-SemiBold.ttf"));
            FontManager.RegisterFont(File.OpenRead("Fonts/Poppins-Bold.ttf"));
        }

        public static class Colors
        {
            public static readonly Color Primary = Color.FromHex("#090e27");
            public static readonly Color Text = Color.FromHex("#212529");
            public static readonly Color Muted = Color.FromHex("#6B7280");
            public static readonly Color Line = Color.FromHex("#E5E7EB");
            public static readonly Color CardBorder = QuestPDF.Helpers.Colors.Grey.Lighten2;

            //public static readonly Color Title = QuestPDF.Helpers.Colors.Blue.Darken4;
        }

        public static void Hr(IContainer c, float thickness = 1)
        {
            c.LineHorizontal(thickness).LineColor(Colors.Line);
        }

        public const float BaseFontSize = 10.2f; 
        public static readonly string FontFamily = "Poppins";

        public static readonly Color TextColor = Colors.Text;   
        public static readonly Color MutedColor = Colors.Muted;  
        public static readonly Color BorderColor = Colors.Line;

        public static TextStyle BaseStyle =>
            TextStyle.Default
                .FontFamily(FontFamily)
                .FontSize(BaseFontSize)
                .FontColor(TextColor)
                .LineHeight(1.6f);

        public static TextStyle H1 =>
            BaseStyle.FontSize(18).SemiBold(); 

        public static TextStyle H2 =>
            BaseStyle.FontSize(14).SemiBold();

        public static TextStyle TitleStyle =>
        BaseStyle.FontSize(24).Bold().FontColor(Colors.Text);

        public static TextStyle HeaderCellStyle =>
            BaseStyle.FontSize(11).SemiBold().FontColor(Colors.Primary);

        public static TextStyle BodyCellStyle =>
            BaseStyle.FontSize(10).FontColor(Colors.Text);

        public static TextStyle MutedStyle =>
            BaseStyle.FontColor(MutedColor).FontSize(9.5f);

        public static TextStyle BoldStyle =>
            BaseStyle.Bold();

        public static TextStyle SectionTitle =>
            BaseStyle.FontSize(12).SemiBold().FontColor(Colors.Primary);

        public static TextStyle Label =>
            BaseStyle.SemiBold();

        public static TextStyle Value =>
            BaseStyle;

        public static TextStyle QuestionNumber =>
            BaseStyle.SemiBold();

        public static TextStyle QuestionText =>
            BaseStyle.FontSize(11).Medium();

        public static TextStyle AnswerText =>
            BaseStyle.FontSize(11);

        public static TextStyle Bullet =>
            BaseStyle.FontSize(11);

        public static TextStyle Observation =>
            BaseStyle.FontSize(11).Italic().FontColor(Colors.Text);

        public static IContainer Card(IContainer c) =>
            c.Border(1).BorderColor(Colors.CardBorder).Padding(12);

        //public static IContainer Justify(this IContainer c) =>
        //    c.Justify();

        public static IContainer SpaceY(this IContainer c, float mm) =>
            c.PaddingVertical(mm);

        public static IContainer CellPadding(this IContainer c)
            => c.PaddingVertical(4).PaddingHorizontal(2);

        public static string FormatDate(DateTime? dt, CultureInfo culture)
            => dt.HasValue ? dt.Value.ToString("dd/MM/yyyy", culture) : "-";

        public static void Field(TextDescriptor t, string label, string? value)
        {
            t.Span(label).Style(Label);
            t.Span(value ?? "—").Style(Value);
        }

        public static void BulletItem(IContainer c, string text)
        {
            c.Row(r =>
            {
                r.ConstantItem(12).Text("•").Style(Bullet);
                r.RelativeItem().Text(text).Style(AnswerText);
            });
        }
    }

    public class BaseA4Document : IDocument
    {
        private readonly string _title;
        private readonly Action<IContainer> _content;

        public BaseA4Document(string title, Action<IContainer> content)
        {
            _title = title;
            _content = content;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);

                page.MarginTop(30, Unit.Millimetre);
                page.MarginBottom(20, Unit.Millimetre);
                page.MarginLeft(20, Unit.Millimetre);
                page.MarginRight(20, Unit.Millimetre);

                page.DefaultTextStyle(PdfTheme.BaseStyle);

                page.Header().Element(ComposeHeader);

                page.Content().Element(c =>
                {
                    _content(c);
                });

                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container
                .Column(col =>
                {
                    col.Spacing(8);

                    col.Item()
                        .AlignCenter()
                        .Text(_title)
                        .Style(PdfTheme.H1);

                    col.Item()
                        .PaddingTop(6)
                        .LineHorizontal(1)
                        .LineColor(PdfTheme.BorderColor);
                });
        }

        private void ComposeFooter(IContainer container)
        {
            container
                .PaddingTop(8)
                .Column(col =>
                {
                    col.Spacing(3);

                    col.Item()
                        .LineHorizontal(1)
                        .LineColor(PdfTheme.BorderColor);

                    col.Item().AlignCenter().Text(text =>
                    {
                        var now = DateTime.Now;
                        text.Span("Documento gerado em ")
                            .Style(PdfTheme.MutedStyle);
                        text.Span(now.ToString("dd/MM/yyyy", new CultureInfo("pt-BR")))
                            .Style(PdfTheme.MutedStyle);
                        text.Span(" às ")
                            .Style(PdfTheme.MutedStyle);
                        text.Span(now.ToString("HH:mm", new CultureInfo("pt-BR")))
                            .Style(PdfTheme.MutedStyle);
                    });

                    col.Item().AlignCenter().Text("Sistema AEE - IPAAGS").Style(PdfTheme.MutedStyle);

                    col.Item().AlignCenter().Text(t =>
                    {
                        t.CurrentPageNumber().Style(PdfTheme.MutedStyle);
                        t.Span(" de ").Style(PdfTheme.MutedStyle);
                        t.TotalPages().Style(PdfTheme.MutedStyle);
                    });
                });
        }
    }

    public static class PdfComponents
    {
        public static void PrintCardsGrid(IContainer container, Action<IContainer>[] cards)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                const float gapMm = 4;

                for (int i = 0; i < cards.Length; i += 2)
                {
                    table.Cell().PaddingRight(gapMm).Element(c =>
                    {
                        c.ShowEntire(); 
                        cards[i](c);
                    });

                    table.Cell().PaddingLeft(gapMm).Element(c =>
                    {
                        c.ShowEntire();
                        if (i + 1 < cards.Length) cards[i + 1](c);
                        else c.Height(0);
                    });
                }
            });
        }

        public static void Card(IContainer container, string title, string body, string? dotHex = null)
        {
            container
                .Border(1)
                .BorderColor(PdfTheme.BorderColor)
                .Padding(10)
                .Column(col =>
                {
                    col.Spacing(6);

                    col.Item().Row(row =>
                    {
                        if (!string.IsNullOrWhiteSpace(dotHex))
                        {
                            row.ConstantItem(10)
                               .Height(10)
                               .Background(dotHex);
                            row.ConstantItem(6).Width(6); 
                        }

                        row.RelativeItem().Text(title).Style(PdfTheme.BoldStyle);
                    });

                    col.Item().Text(body).Justify();
                });
        }
    }
}
