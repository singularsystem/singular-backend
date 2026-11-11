using Amazon.Runtime.EventStreams;
using SingularSystem.Extensions;
using SingularSystem.Interfaces;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Templates;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace SingularSystem.Services
{
    public class PEIPDFGenerator : IDocumentPdfGenerator
    {
        private readonly IPEISuggestionLoader _suggestions;
        private readonly IPortageSuggestionLoader _portageSuggestions;

        public PEIPDFGenerator(
            IPEISuggestionLoader suggestions,
            IPortageSuggestionLoader portageSuggestions)
        {
            _suggestions = suggestions;
            _portageSuggestions = portageSuggestions;
        }
        public string Discriminator => "PEI";

        public Task<byte[]> GenerateAsync(BaseDocument document, PdfRenderContext context, CancellationToken ct = default)
        {
            var pei = (PEI)document;
            var serie = pei.Serie;
           
            var banksBySubject = pei.Subjects
                .Select(s => s.Subject)
                .Distinct()
                .ToDictionary(
                    sub => sub,
                    sub => sub == Subject.EarlyChildhoodEducation
                        ? new Dictionary<string, PEISuggestionQuestion>(StringComparer.OrdinalIgnoreCase)
                        : _suggestions.Load(sub, serie)
                );

            var bytes = new PEIPDFDocument(pei, banksBySubject, _portageSuggestions, context).GeneratePdf();
            return Task.FromResult(bytes);
        }

        public Task<byte[]> GenerateAsync(BaseDocument document, CancellationToken ct = default)
            => GenerateAsync(document, PdfRenderContext.Default(), ct);

        private static string DashIfEmpty(string? text)
             => string.IsNullOrWhiteSpace(text) ? "-" : text.Trim();

        public class PEIPDFDocument : IDocument
        {
            private readonly PEI _pei;
            private readonly IReadOnlyDictionary<Subject, IReadOnlyDictionary<string, PEISuggestionQuestion>> _banks;
            private readonly IPortageSuggestionLoader _portageSuggestions;
            private readonly PdfRenderContext _context;

            public PEIPDFDocument(
                PEI pei,
                IReadOnlyDictionary<Subject, IReadOnlyDictionary<string, PEISuggestionQuestion>> banks,
                IPortageSuggestionLoader portageSuggestions,
                PdfRenderContext? context = null)
            {
                _pei = pei;
                _banks = banks;
                _portageSuggestions = portageSuggestions;
                _context = context ?? PdfRenderContext.Default();
            }

            public DocumentMetadata GetMetadata() => PdfMetadataFactory.CreateMetadata(_context.Profile, "Plano Educacional Individualizado");
            public DocumentSettings GetSettings() => PdfMetadataFactory.CreateSettings(_context.Profile);

            public void Compose(IDocumentContainer container)
            {
                var culture = new CultureInfo("pt-BR");

                var subjectsSection = new PEISubjectsSection
                {
                    Subjects = _pei.Subjects.Select(s =>
                    {
                        _banks.TryGetValue(s.Subject, out var bank);
                        bank ??= new Dictionary<string, PEISuggestionQuestion>(StringComparer.OrdinalIgnoreCase);

                        var responses = s.Responses.Select(r =>
                        {
                            if (TryParseEarlyChildhoodKey(s.Subject, r.Key, out var ageRange, out var indexKey))
                            {
                                var earlyChildhoodBank = _portageSuggestions.LoadBank(ageRange);
                                earlyChildhoodBank.TryGetValue(indexKey, out var earlyChildhoodQuestion);

                                var earlyChildhoodResponseText = r.Response.GetDisplayName();
                                var earlyChildhoodSuggestion = _portageSuggestions.GetSuggestion(ageRange, indexKey, r.Response) ?? "";
                                var earlyChildhoodCategory = NormalizeCategory(r.Category, earlyChildhoodQuestion?.Category);

                                return new PEISubjectResponse
                                {
                                    Key = r.Key ?? "",
                                    Response = earlyChildhoodResponseText,
                                    Category = earlyChildhoodCategory,
                                    Question = earlyChildhoodQuestion?.Question ?? "",
                                    Suggestion = earlyChildhoodSuggestion
                                };
                            }

                            bank.TryGetValue(r.Key ?? "", out var q);

                            var responseText = r.Response.GetDisplayName();

                            var suggestion = PickSuggestion(responseText, q);
                            var category = NormalizeCategory(r.Category, q?.Tipo);

                            return new PEISubjectResponse
                            {
                                Key = r.Key ?? "",
                                Response = responseText,
                                Category = category,
                                Question = q?.Pergunta ?? "",
                                Suggestion = suggestion ?? ""
                            };
                        }).ToList();

                        var total = responses.Count;
                        var generated = responses.Count(r => !string.IsNullOrWhiteSpace(r.Suggestion));

                        return new PEISubject
                        {
                            Subject = s.Subject.GetDisplayName(),
                            SerieDisplay = _pei.Serie.GetDisplayName(),
                            Responses = responses,
                            TotalSkills = total,
                            SuggestionsGenerated = generated
                        };
                    }).ToList()
                };

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(_ => PdfTheme.BaseStyle);

                    page.Header().ShowOnce().Element(h =>
                    {
                        PdfBrandChrome.ComposeHeader(h, _context, "Plano Educacional Individualizado");
                    });

                    page.Content()
                        .PaddingTop(8)
                        .PaddingBottom(16)
                        .Column(x =>
                        {
                            x.Item().Column(section =>
                            {
                                section.Spacing(8);

                                section.Item()
                                    .Element(PdfTheme.Card)
                                    .Column(card =>
                                    {
                                        card.Spacing(8);

                                        card.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text(t =>
                                                PdfTheme.Field(t, "Nome: ", _pei?.Student?.FullName));

                                            row.RelativeItem().Text(t =>
                                            {
                                                var dob = _pei?.Student?.DateOfBirth;
                                                var dobText = dob.HasValue ? dob.Value.ToString("dd/MM/yyyy") : "—";
                                                PdfTheme.Field(t, "Data de Nascimento: ", dobText);
                                            });
                                        });

                                        var hasState = !string.IsNullOrWhiteSpace(_pei?.Student?.State);
                                        var hasCity = !string.IsNullOrWhiteSpace(_pei?.Student?.City);
                                        if (hasState || hasCity)
                                        {
                                            card.Item().Row(row =>
                                            {
                                                if (hasState)
                                                {
                                                    row.RelativeItem().Text(t =>
                                                        PdfTheme.FieldIfPresent(t, "Estado: ", _pei?.Student?.State));
                                                }
                                                if (hasCity)
                                                {
                                                    row.RelativeItem().Text(t =>
                                                        PdfTheme.FieldIfPresent(t, "Município: ", _pei?.Student?.City));
                                                }
                                            });
                                        }

                                        var hasSerie = _pei?.Student?.Serie != null;
                                        var hasSchool = !string.IsNullOrWhiteSpace(_pei?.Student?.School?.Name);
                                        if (hasSerie || hasSchool)
                                        {
                                            card.Item().Row(row =>
                                            {
                                                if (hasSerie)
                                                {
                                                    row.RelativeItem().Text(t =>
                                                        PdfTheme.Field(t, "Ano/Série: ", _pei?.Student?.Serie.GetDisplayName()));
                                                }
                                                if (hasSchool)
                                                {
                                                    row.RelativeItem().Text(t =>
                                                        PdfTheme.FieldIfPresent(t, "Escola: ", _pei?.Student?.School?.Name));
                                                }
                                            });
                                        }

                                        card.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text(t =>
                                                PdfTheme.Field(t, "Profissional: ", _pei?.User?.FullName));
                                        });
                                    });
                            });

                            if (_pei.Serie == Serie.EarlyChildhoodEducation)
                            {
                                x.Item().PaddingTop(10).Column(section =>
                                {
                                    section.Spacing(8);

                                    section.Item()
                                        .Element(PdfTheme.Card)
                                        .Column(card =>
                                        {
                                            card.Spacing(6);
                                            card.Item().Text("Resumo da Avaliação").FontSize(12).SemiBold();

                                            var resumo = BuildEarlyChildhoodSummary(_pei);
                                            foreach (var dominio in resumo.Keys.OrderBy(k => k))
                                            {
                                                var counts = resumo[dominio];
                                                var total = counts["Ainda Não"] + counts["Com Mediação"] + counts["Sozinho"];
                                                card.Item().Text($"{dominio} — {total} questões avaliadas").SemiBold();
                                                card.Item().Text($"Ainda Não: {counts["Ainda Não"]}");
                                                card.Item().Text($"Com Mediação: {counts["Com Mediação"]}");
                                                card.Item().Text($"Sozinho: {counts["Sozinho"]}");
                                            }
                                        });
                                });
                            }

                            x.Item().PaddingTop(10).Column(section =>
                            {
                                section.Spacing(8);

                                section.Item()
                                    .Element(PdfTheme.Card)
                                    .Column(card =>
                                    {
                                        card.Spacing(6);

                                        card.Item().Text("Informações adicionais").FontSize(12).SemiBold();

                                        card.Item().Text(t =>
                                            PdfTheme.Field(t, "Histórico Escolar: ", DashIfEmpty(_pei?.AcademicRecord)));
                                        card.Item().Text(t =>
                                            PdfTheme.Field(t, "Tem Laudo Médico? Qual CID-10?: ", DashIfEmpty(_pei?.MedicalReport)));
                                        card.Item().Text(t =>
                                            PdfTheme.Field(t, "Usa Medicações? Quais?: ", DashIfEmpty(_pei?.Medications)));
                                        card.Item().Text(t =>
                                            PdfTheme.Field(t, "Faz Acompanhamento? Reabilitação?: ", DashIfEmpty(_pei?.FollowUp)));
                                        card.Item().Text(t =>
                                            PdfTheme.Field(t, "Observações Adicionais: ", DashIfEmpty(_pei?.AdditionalObservations)));
                                    });
                            });

                            x.Item().PaddingBottom(16);

                            x.Item().Element(c => ComposeSubjectsBody(c, subjectsSection));
                        });


                    page.Footer().Element(f =>
                    {
                        PdfBrandChrome.ComposeFooter(f, _context);
                    });
                });
            }

            private static string NormalizeCategory(string? original, string? fallbackTipo)
            {
                if (!string.IsNullOrWhiteSpace(original))
                {
                    var parts = original.Split('•', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length >= 2) return parts[1];
                    return original.Trim();
                }

                return fallbackTipo?.Trim() ?? "-";
            }

            private static string? PickSuggestion(string responseDisplay, PEISuggestionQuestion? q)
            {
                if (q is null) return null;

                if (IsNotYet(responseDisplay)) return q.SugestaoNao;
                if (IsWithMediation(responseDisplay)) return q.SugestaoSimComMediacao;

                return null; 
            }

            private static bool IsNotYet(string s)
                => s.Equals("Ainda Não", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Ainda Nao", StringComparison.OrdinalIgnoreCase);

            private static bool IsWithMediation(string s)
                => s.Equals("Com Mediação", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Com Mediacao", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Sim com mediação", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Sim com mediacao", StringComparison.OrdinalIgnoreCase);

            private static bool TryParseEarlyChildhoodKey(
                Subject subject,
                string? rawKey,
                out AgeRange ageRange,
                out string indexKey)
            {
                ageRange = AgeRange.None;
                indexKey = "";

                if (subject != Subject.EarlyChildhoodEducation)
                    return false;

                if (string.IsNullOrWhiteSpace(rawKey))
                    return false;

                var parts = rawKey.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length < 4 || !parts[0].Equals("EI", StringComparison.OrdinalIgnoreCase))
                    return false;

                var faixa = $"{parts[1]}-{parts[2]}";
                indexKey = parts[3];

                ageRange = faixa switch
                {
                    "0-1" => AgeRange.ZeroOneYear,
                    "1-2" => AgeRange.OneTwoYears,
                    "2-3" => AgeRange.TwoThreeYears,
                    "3-4" => AgeRange.ThreeFourYears,
                    "4-5" => AgeRange.FourFiveYears,
                    _ => AgeRange.None
                };

                return ageRange != AgeRange.None && !string.IsNullOrWhiteSpace(indexKey);
            }

            private static Dictionary<string, Dictionary<string, int>> BuildEarlyChildhoodSummary(PEI pei)
            {
                var summary = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

                foreach (var subject in pei.Subjects.Where(s => s.Subject == Subject.EarlyChildhoodEducation))
                {
                    foreach (var response in subject.Responses)
                    {
                        var category = NormalizeCategory(response.Category, null);
                        if (string.IsNullOrWhiteSpace(category) || category == "-")
                            continue;

                        if (!summary.ContainsKey(category))
                        {
                            summary[category] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["Ainda Não"] = 0,
                                ["Com Mediação"] = 0,
                                ["Sozinho"] = 0
                            };
                        }

                        var label = response.Response.GetDisplayName();
                        if (summary[category].ContainsKey(label))
                        {
                            summary[category][label]++;
                        }
                    }
                }

                return summary;
            }

            public void ComposeSubjectsBody(IContainer container, PEISubjectsSection pei)
            {
                container.PaddingTop(8).Column(col =>
                {
                    if (pei.Subjects == null || pei.Subjects.Count == 0)
                    {
                        col.Item().Text("Nenhuma disciplina cadastrada.").Italic();
                        return;
                    }

                    foreach (var subject in pei.Subjects)
                    {
                        col.Item().Element(x => SubjectBlock(x, subject));
                        col.Item().PaddingBottom(12);
                    }
                });
            }

            private void SubjectBlock(IContainer container, PEISubject subject)
            {
                container.Border(1).BorderColor(PdfTheme.Colors.Line).Padding(10).Column(col =>
                {
                    col.Item().Text(DashIfEmpty(subject.Subject)).FontSize(13).SemiBold();
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(PdfTheme.Colors.Line);

                    col.Item().PaddingTop(4)
                       .Text($"{DashIfEmpty(subject.SerieDisplay)} • Sugestões geradas: {subject.SuggestionsGenerated}/{subject.TotalSkills}")
                       .FontSize(9)
                       .Style(PdfTheme.MutedStyle);

                    if (subject.SuggestionsGenerated == 0)
                    {
                        col.Item().PaddingTop(10).Text("Sem sugestões").FontSize(10).SemiBold();

                        col.Item().PaddingTop(6)
                            .Text("Nenhuma sugestão para exibir.")
                            .FontSize(9)
                            .Style(PdfTheme.MutedStyle);

                        return;
                    }

                    var withSuggestions = (subject.Responses ?? new List<PEISubjectResponse>())
                        .Where(r => !string.IsNullOrWhiteSpace(r.Suggestion))
                        .ToList();

                    var grouped = withSuggestions
                        .GroupBy(r => DashIfEmpty(r.Category))
                        .ToList();

                    if (grouped.Count == 0)
                    {
                        col.Item().PaddingTop(8).Text("Sem respostas nesta disciplina.").Italic();
                        return;
                    }

                    foreach (var group in grouped)
                    {
                        col.Item().PaddingTop(10).Text(group.Key).FontSize(10).SemiBold();

                        col.Item().PaddingTop(6).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Habilidade");
                                h.Cell().Element(HeaderCell).Text("Resposta");

                                h.Cell().ColumnSpan(2)
                                    .BorderBottom(1)
                                    .BorderColor(PdfTheme.Colors.Line)
                                    .PaddingTop(4);
                            });

                            var rows = group.OrderBy(r => r.Key).ToList();

                            for (int i = 0; i < rows.Count; i++)
                            {
                                var r = rows[i];
                                var isEven = i % 2 == 0;

                                var abilityText = !string.IsNullOrWhiteSpace(r.Question) ? r.Question : r.Key;

                                t.Cell().Element(c => BodyCell(c, isEven))
                                    .AlignCenter()
                                    .Text(abilityText)
                                    .FontSize(9);

                                t.Cell().Element(c => BodyCell(c, isEven))
                                    .Text(DashIfEmpty(r.Response)).AlignCenter();

                                if (!string.IsNullOrWhiteSpace(r.Suggestion))
                                {
                                    t.Cell().ColumnSpan(2)
                                        .BorderBottom(1)
                                        .BorderColor(PdfTheme.Colors.Line)
                                        .Padding(8)
                                        .Text(txt =>
                                        {
                                            txt.Span("Sugestão: ").Style(PdfTheme.MutedStyle);
                                            txt.Span(r.Suggestion);
                                        });
                                }
                            }
                        });
                    }
                });
            }

            private static IContainer HeaderCell(IContainer c)
                => c.AlignCenter().AlignMiddle().DefaultTextStyle(_ => PdfTheme.HeaderCellStyle);

            private static IContainer BodyCell(IContainer c, bool isEvenRow)
                => c.DefaultTextStyle(_ => PdfTheme.BodyCellStyle).CellPadding();
        }

        public class PEISubjectsSection
        {
            public List<PEISubject> Subjects { get; set; } = new();
        }

        public class PEISubject
        {
            public string Subject { get; set; } = "";
            public List<PEISubjectResponse> Responses { get; set; } = new();

            public string SerieDisplay { get; set; } = "";
            public int TotalSkills { get; set; }
            public int SuggestionsGenerated { get; set; }
        }

        public class PEISubjectResponse
        {
            public string Key { get; set; } = "";
            public string Response { get; set; } = "";
            public string Category { get; set; } = "";
            public string Question { get; set; } = "";
            public string Suggestion { get; set; } = "";
        }

        public sealed class PEISuggestionFile
        {
            public string Subject { get; set; }
            public string Serie { get; set; }
            public List<PEISuggestionQuestion> Questions { get; set; } = new();
        }

        public sealed class PEISuggestionQuestion
        {
            public string Id { get; set; } = "";
            public string Tipo { get; set; } = "";
            public string Pergunta { get; set; } = "";
            public string SugestaoNao { get; set; } = "";
            public string SugestaoSimComMediacao { get; set; } = "";
        }
    }

    public static class PEISuggestionPathResolver
    {
        public static string Resolve(
            string contentRootPath,
            Subject subject,
            Serie serie)
        {
            var subjectFolder = ResolveSubjectFolder(subject);
            var serieFile = ResolveSerieFile(serie);

            return Path.Combine(
                contentRootPath,
                "Resources",
                "PEI",
                subjectFolder,
                serieFile
            );
        }

        private static string ResolveSubjectFolder(Subject subject)
        {
            return subject switch
            {
                Subject.Mathematics => "Matematica",
                Subject.Portuguese => "Lingua Portuguesa",
                Subject.Science => "Ciencias",
                Subject.History => "Historia",
                Subject.Geography => "Geografia",
                Subject.English => "Ingles",
                Subject.EarlyChildhoodEducation => "Educacao Infantil",
                _ => throw new ArgumentOutOfRangeException(nameof(subject), subject, "Disciplina não mapeada")
            };
        }

        private static string ResolveSerieFile(Serie serie)
        {
            return serie switch
            {
                Serie.EarlyChildhoodEducation => "educacao-infantil.json",
                Serie.FirstYear => "1ano.json",
                Serie.SecondYear => "2ano.json",
                Serie.ThirdYear => "3ano.json",
                Serie.FourthYear => "4ano.json",
                Serie.FifthYear => "5ano.json",
                Serie.SixthYear => "6ano.json",
                Serie.SeventhYear => "7ano.json",
                Serie.EighthYear => "8ano.json",
                Serie.NinthYear => "9ano.json",
                _ => throw new ArgumentOutOfRangeException(nameof(serie), serie, "Série não mapeada")
            };
        }
    }
}
