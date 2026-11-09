using SingularSystem.Extensions;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Templates;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace SingularSystem.Services
{
    public class PAEEPDFGenerator : IDocumentPdfGenerator
    {
        public string Discriminator => "PAEE";

        private readonly IPortageSuggestionLoader _portageSuggestions;

        public PAEEPDFGenerator(IPortageSuggestionLoader portageSuggestions)
        {
            _portageSuggestions = portageSuggestions;
        }

        public Task<byte[]> GenerateAsync(BaseDocument document, CancellationToken ct = default)
        {
            var paee = (PAEE)document;
            var bytes = new PAEEPDFDocument(paee, _portageSuggestions).GeneratePdf();
            return Task.FromResult(bytes);
        }

        private sealed class PAEEPDFDocument : IDocument
        {
            private readonly PAEE _paee;
            private readonly IPortageSuggestionLoader _portageSuggestions;

            private static readonly string[] _categoriasSugestoes = new[]
            {
                "Socialização",
                "Linguagem",
                "Cognição",
                "Auto-cuidados",
                "Desenvolvimento Motor",
                "Percepção",
                "Atenção",
                "Memória",
                "Raciocínio Lógico",
                "Leitura e Escrita",
                "Pensamento Operatório",
                "Funções Psicomotoras"
            };

            public PAEEPDFDocument(PAEE paee, IPortageSuggestionLoader portageSuggestions)
            {
                _paee = paee;
                _portageSuggestions = portageSuggestions;
            }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public void Compose(IDocumentContainer container)
            {
                var culture = new CultureInfo("pt-BR");

                var summaries = ComputeDomainSummaries(_paee?.Portage);

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(_ => PdfTheme.BaseStyle);

                    page.Header().ShowOnce().Element(Header);

                    page.Content()
                        .PaddingTop(8)
                        .PaddingBottom(16)
                        .Column(col =>
                        {
                            col.Item().Element(StudentCard);
                            col.Item().PaddingBottom(16);

                            col.Item().PaddingBottom(12).Element(c => SummaryCardsSection(c, summaries));

                            col.Item().PaddingBottom(12).Element(c => TextSection(c, "2. Histórico Escolar", _paee?.AcademicRecord));
                            col.Item().PaddingBottom(12).Element(c => TextSection(c, "3. Tem Laudo Médico? Qual CID-10?", _paee?.MedicalReport));
                            col.Item().PaddingBottom(12).Element(c => TextSection(c, "4. Usa Medicação? Quais?", _paee?.Medications));
                            col.Item().PaddingBottom(12).Element(c => TextSection(c, "5. Faz Acompanhamento? Reabilitação?", _paee?.FollowUp));

                            col.Item().PaddingBottom(12).Element(SuggestionsSection);

                            col.Item().PaddingBottom(12).Element(OrganizationOfCareSection);
                            col.Item().PaddingBottom(12).Element(WeeklyFrequencySection);
                            col.Item().PaddingBottom(12).Element(ServiceTimeSection);

                            col.Item().PaddingBottom(12).Element(c => TextSection(c, "10. Composição do Atendimento", _paee?.CompositionOfAssisting));
                            col.Item().PaddingBottom(12).Element(OtherProfessionalsSection);

                            col.Item().PaddingBottom(12).Element(c => BulletsSection(c, "12. Atividades Diferenciadas",
                                _paee?.DifferentiatedActivitiesInAEE.Select(x => x.Activity).ToList()));

                            col.Item().PaddingBottom(12).Element(c => BulletsSection(c, "13. Metodologia de Trabalho",
                                _paee?.WorkMethodology.Select(x => x.Methodology).ToList()));

                            col.Item().PaddingBottom(12).Element(c => BulletsSection(c, "14. Recursos Materiais e Equipamentos",
                                _paee?.MaterialResourcesAndEquipment.Select(x => x.Resource).ToList()));

                            col.Item().PaddingBottom(12).Element(c => BulletsSection(c, "15. Critérios de Avaliação",
                                _paee?.CriteriaInEvaluation.Select(x => x.Criterion).ToList()));

                            col.Item().PaddingBottom(12).Element(c => BulletsSection(c, "16. Recursos e Estratégias",
                                _paee?.ResourcesAndStrategies.Select(x => x.Strategy).ToList()));

                            col.Item().PaddingBottom(12).Element(c => TextSection(c, "17. Observações Adicionais", DashIfEmpty(_paee?.AdditionalObservations)));
                        });

                    page.Footer().Element(Footer);
                });
            }

            private void Header(IContainer container)
            {
                container.Column(col =>
                {
                    col.Item().AlignCenter().Text("IPAAGS · Sistema de Gestão Educacional").Style(PdfTheme.MutedStyle);
                    col.Item().PaddingTop(4);

                    col.Item().AlignCenter().Text("Plano de Atendimento Educacional Especializado").Style(PdfTheme.TitleStyle);

                    col.Item().PaddingTop(48);
                    col.Item().Element(x => PdfTheme.Hr(x, 2));
                });
            }

            private void Footer(IContainer container)
            {
                container.PaddingTop(6).Column(col =>
                {
                    col.Item().PaddingBottom(6).Element(x => PdfTheme.Hr(x, 2));

                    col.Item().AlignCenter()
                        .Text($"Documento gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm:ss}")
                        .Style(PdfTheme.MutedStyle);

                    col.Item().AlignCenter().Text("Sistema AEE - IPAAGS").Style(PdfTheme.MutedStyle);

                    col.Item().AlignCenter().Text(t =>
                    {
                        t.Span("Página ").Style(PdfTheme.MutedStyle);
                        t.CurrentPageNumber().Style(PdfTheme.MutedStyle);
                        t.Span(" de ").Style(PdfTheme.MutedStyle);
                        t.TotalPages().Style(PdfTheme.MutedStyle);
                    });
                });
            }

            private void StudentCard(IContainer container)
            {
                container.Element(PdfTheme.Card).Column(card =>
                {
                    card.Spacing(8);

                    card.Item().Row(row =>
                    {
                        row.RelativeItem().Text(t => PdfTheme.Field(t, "Nome: ", _paee?.Student?.FullName));

                        row.RelativeItem().Text(t =>
                        {
                            var dob = _paee?.Student?.DateOfBirth;
                            var dobText = dob.HasValue ? dob.Value.ToString("dd/MM/yyyy") : "—";
                            PdfTheme.Field(t, "Data de Nascimento: ", dobText);
                        });
                    });

                    card.Item().Row(row =>
                    {
                        row.RelativeItem().Text(t => PdfTheme.Field(t, "Estado: ", _paee?.Student?.State));
                        row.RelativeItem().Text(t => PdfTheme.Field(t, "Município: ", _paee?.Student?.City));
                    });

                    card.Item().Row(row =>
                    {
                        row.RelativeItem().Text(t => PdfTheme.Field(t, "Ano/Série: ", _paee?.Student?.Serie.GetDisplayName()));
                        row.RelativeItem().Text(t => PdfTheme.Field(t, "Escola: ", _paee?.Student?.School?.Name));
                    });

                    card.Item().Row(row =>
                    {
                        row.RelativeItem().Text(t => PdfTheme.Field(t, "Profissional: ", _paee?.User?.FullName));
                    });
                });
            }

            private void SuggestionsSection(IContainer container)
            {
                Section(container, "6. Sugestões de Intervenção Pedagógica por Área de Desenvolvimento", c =>
                {
                    var ageRange = _paee.Portage?.PortageAgeRange ?? AgeRange.None;
                    var responses = _paee.Portage?.Responses ?? new List<PortageGuideResponse>();

                    var groups = _categoriasSugestoes
                        .Select(cat =>
                        {
                            var suggestions = responses
                                .Where(r => string.Equals(r.Value?.Category, cat, StringComparison.OrdinalIgnoreCase))
                                .Select(r => _portageSuggestions.GetSuggestion(ageRange, r.Key, r.Value.Response))
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Select(s => s!.Trim())
                                .ToList();

                            return (Category: cat, Suggestions: suggestions);
                        })
                        .Where(g => g.Suggestions.Count > 0)
                        .ToList();

                    if (groups.Count == 0)
                    {
                        c.Text("Sem sugestões.").FontColor(Colors.Grey.Darken2);
                        return;
                    }

                    c.Column(col =>
                    {
                        col.Spacing(10);

                        foreach (var g in groups)
                        {
                            col.Item().Text(g.Category).FontSize(11).SemiBold();

                            col.Item().Column(list =>
                            {
                                list.Spacing(4);

                                foreach (var s in g.Suggestions)
                                {
                                    list.Item().Row(r =>
                                    {
                                        r.ConstantItem(12).Text("•");
                                        r.RelativeItem().Text(s).FontColor(Colors.Grey.Darken4);
                                    });
                                }
                            });
                        }
                    });
                });
            }

            private void OrganizationOfCareSection(IContainer container)
            {
                var items = _paee?.OrganizationOfCare
                    .Select(x => x.Type.GetDisplayName())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList() ?? new List<string>();

                items = WithOtherBullet(items, "Outro", _paee?.OtherOrganization);

                BulletsSection(container, "7. Tipo de Atendimento Educacional Especializado", items);
            }

            private void WeeklyFrequencySection(IContainer container)
            {
                var weekly = _paee?.WeeklyFrequency.GetDisplayName() ?? "—";
                weekly = BuildEnumWithOtherText(weekly, "Outra", _paee?.OtherFrequency);

                TextSection(container, "8. Frequência Semanal", weekly);
            }

            private void ServiceTimeSection(IContainer container)
            {
                var time = _paee?.AssistingTimePerSession.GetDisplayName() ?? "—";
                time = BuildEnumWithOtherText(time, "Outro", _paee?.OtherServiceTime);

                TextSection(container, "9. Tempo de Atendimento por Sessão", time);
            }

            private void OtherProfessionalsSection(IContainer container)
            {
                var items = _paee.OtherProfessionals
                    .Select(x => x.Professional.GetDisplayName())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                items = WithOtherBullet(items, "Outro", _paee.OtherProfessionalsDescription);

                Section(container, "11. Outros Profissionais Envolvidos", c =>
                {
                    if (items.Count == 0)
                    {
                        c.Text("—").FontColor(Colors.Grey.Darken2);
                        return;
                    }

                    c.Column(col =>
                    {
                        col.Spacing(4);
                        foreach (var s in items)
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(12).Text("•");
                                r.RelativeItem().Text(Normalize(s)).FontColor(Colors.Grey.Darken4);
                            });
                        }
                    });
                });
            }

            private void SummaryCardsSection(IContainer container, List<DomainSummary> summaries)
            {
                Section(container, "1. Resumo da Avaliação", c =>
                {
                    if (summaries == null || summaries.Count == 0)
                    {
                        c.Text("—").FontColor(Colors.Grey.Darken2);
                        return;
                    }

                    c.Table(t =>
                    {
                        t.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                        });

                        foreach (var s in summaries)
                        {
                            t.Cell()
                                .Padding(6)
                                .Border(1)
                                .BorderColor("#E5E7EB")
                                .Padding(10)
                                .Column(col =>
                                {
                                    col.Spacing(6);

                                    col.Item().Text(s.Domain).SemiBold().FontSize(11);

                                    col.Item().Text($"{s.Total} questões avaliadas")
                                        .FontColor(Colors.Grey.Darken2)
                                        .FontSize(9);

                                    col.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text($"Ainda Não: {s.AindaNao}").FontSize(9);
                                        r.RelativeItem().Text($"Com Mediação: {s.ComMediacao}").FontSize(9);
                                    });

                                    col.Item().Text($"Sozinho: {s.Sozinho}").FontSize(9);
                                });
                        }
                    });
                });
            }

            private static List<DomainSummary> ComputeDomainSummaries(PortageGuide? portage)
            {
                var items = portage?.Responses ?? new List<PortageGuideResponse>();
                if (items.Count == 0) return new();

                static string NormCat(string? c) => string.IsNullOrWhiteSpace(c) ? "—" : c.Trim();

                static bool IsAindaNao(PlanResponse r)
                    => r.GetDisplayName().Equals("Ainda Não", StringComparison.OrdinalIgnoreCase)
                    || r.GetDisplayName().Equals("Ainda Nao", StringComparison.OrdinalIgnoreCase);

                static bool IsComMediacao(PlanResponse r)
                    => r.GetDisplayName().Equals("Com Mediação", StringComparison.OrdinalIgnoreCase)
                    || r.GetDisplayName().Equals("Com Mediacao", StringComparison.OrdinalIgnoreCase);

                static bool IsSozinho(PlanResponse r)
                    => r.GetDisplayName().Equals("Sozinho", StringComparison.OrdinalIgnoreCase);

                return items
                    .Where(x => x.Value != null)
                    .GroupBy(x => NormCat(x.Value.Category))
                    .Select(g =>
                    {
                        var total = g.Count();
                        var aindaNao = g.Count(x => IsAindaNao(x.Value.Response));
                        var comMediacao = g.Count(x => IsComMediacao(x.Value.Response));
                        var sozinho = g.Count(x => IsSozinho(x.Value.Response));

                        return new DomainSummary(g.Key, total, aindaNao, comMediacao, sozinho);
                    })
                    .OrderBy(x => x.Domain)
                    .ToList();
            }

            private static void Section(IContainer container, string title, Action<IContainer> content)
            {
                container.Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text(title).FontSize(12).SemiBold();
                    col.Item().Element(content);
                });
            }

            private static void TextSection(IContainer container, string title, string? text)
            {
                Section(container, title, c =>
                {
                    c.Text(Normalize(text)).FontColor(Colors.Grey.Darken4).Justify();
                });
            }

            private static void BulletsSection(IContainer container, string title, IEnumerable<string>? items)
            {
                Section(container, title, c =>
                {
                    var list = (items ?? Enumerable.Empty<string>())
                        .Select(x => x?.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();

                    if (list.Count == 0)
                    {
                        c.Text("—").FontColor(Colors.Grey.Darken2);
                        return;
                    }

                    c.Column(col =>
                    {
                        col.Spacing(4);
                        foreach (var item in list)
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(12).Text("•");
                                r.RelativeItem().Text(item!).FontColor(Colors.Grey.Darken4);
                            });
                        }
                    });
                });
            }

            private static string Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? "—" : text.Trim();
        }
        private static string DashIfEmpty(string? text)
            => string.IsNullOrWhiteSpace(text) ? "-" : text.Trim();

        private static string ComposeOtherLine(string otherLabel, string? otherText)
        {
            if (string.IsNullOrWhiteSpace(otherText)) return "—";
            return $"{otherLabel} - {otherText.Trim()}";
        }

        private static bool IsOtherDisplay(string? display)
        {
            if (string.IsNullOrWhiteSpace(display)) return false;

            return display.Trim().Equals("Outro", StringComparison.OrdinalIgnoreCase)
                || display.Trim().Equals("Outra", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildEnumWithOtherText(string enumDisplay, string otherLabel, string? otherText)
        {
            if (!IsOtherDisplay(enumDisplay))
                return enumDisplay;

            return ComposeOtherLine(otherLabel, otherText);
        }

        private static List<string> WithOtherBullet(List<string> baseItems, string otherLabel, string? otherText)
        {
            var hasOtherSelected = baseItems.Any(x =>
                x != null && x.Trim().Equals(otherLabel, StringComparison.OrdinalIgnoreCase));

            if (!hasOtherSelected)
                return baseItems;

            baseItems = baseItems
                .Where(x => x == null || !x.Trim().Equals(otherLabel, StringComparison.OrdinalIgnoreCase))
                .ToList();

            baseItems.Add(ComposeOtherLine(otherLabel, otherText));

            return baseItems;
        }

        public record DomainSummary(
            string Domain,
            int Total,
            int AindaNao,
            int ComMediacao,
            int Sozinho
        );
    }
}
