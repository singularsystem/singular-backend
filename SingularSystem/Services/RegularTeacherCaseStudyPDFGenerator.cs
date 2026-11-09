using Amazon.S3.Model;
using SingularSystem.DTOs.Students;
using SingularSystem.Extensions;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Templates;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace SingularSystem.Services
{
    public class RegularTeacherCaseStudyPDFGenerator : IDocumentPdfGenerator
    {
        public string Discriminator => "RegularTeacherEvaluationDocument";

        public Task<byte[]> GenerateAsync(BaseDocument document, CancellationToken ct = default)
        {
            var evaluation = (RegularTeacherEvaluationDocument)document;
            var bytes = new RegularTeacherEvaluationPDFDocument(evaluation).GeneratePdf();
            return Task.FromResult(bytes);
        }

        public class RegularTeacherEvaluationPDFDocument : IDocument
        {

            private readonly RegularTeacherEvaluationDocument _evaluation;
            public RegularTeacherEvaluationPDFDocument(RegularTeacherEvaluationDocument evaluation) => _evaluation = evaluation;
            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public void Compose(IDocumentContainer container)
            {
                var culture = new CultureInfo("pt-BR");

                var items = new List<QuestionAnswer>
                {
                    new() { Order = 1, Question = "Como é o desenvolvimento do aluno da sala de aula?", Answer = _evaluation.RegularTeacherQuestionnaire.ClassroomDevelopment },
                    new() { Order = 2, Question = "Como é o comportamento do aluno na sala de aula?", Answer = _evaluation.RegularTeacherQuestionnaire.ClassroomBehavior },
                    new() { Order = 3, Question = "Quais as principais dificuldades apresentadas pelo aluno?", Answer = _evaluation.RegularTeacherQuestionnaire.MainDifficulties },
                    new() { Order = 4, Question = "Quais as suas características quanto á aprendizagem e assimilação de conteúdos?", Answer = _evaluation.RegularTeacherQuestionnaire.LearningCharacteristics },
                    new() { Order = 5, Question = "Como você descreveria a leitura e escrita do aluno?", Answer = _evaluation.RegularTeacherQuestionnaire.ReadingAndWritingDescription },
                    new() { Order = 6, Question = "Como você descreveria o raciocínio lógico matemático do aluno?", Answer = _evaluation.RegularTeacherQuestionnaire.LogicalMathematicalReasoningDescription },
                    new() { Order = 7, Question = "Faz as atividades escolares?", Answer = _evaluation.RegularTeacherQuestionnaire.CompletesSchoolActivities },
                    new() { Order = 8, Question = "Faz as atividades de casa?", Answer = _evaluation.RegularTeacherQuestionnaire.CompletesHomework },
                    new() { Order = 9, Question = "Como reage quando é contrariado?", Answer = _evaluation.RegularTeacherQuestionnaire.ReactionToFrustration },
                    new() { Order = 10, Question = "O aluno é inconveniente? Em que circunstâncias demonstra inconveniência? E como se manifesta?", Answer = _evaluation.RegularTeacherQuestionnaire.InappropriateBehaviorDescription },
                    new() { Order = 11, Question = "Tem dificuldade em trabalhar em grupo? Como se manifesta essa dificuldade?", Answer = _evaluation.RegularTeacherQuestionnaire.DifficultyWorkingInGroup },
                    new() { Order = 12, Question = "Tem dificuldade em organizar suas tarefas e atividades pessoais?", Answer = _evaluation.RegularTeacherQuestionnaire.DifficultyOrganizingTasks },
                    new() { Order = 13, Question = "Os colegas da turma o aceitam?", Answer = _evaluation.RegularTeacherQuestionnaire.PeerAcceptance },
                    new() { Order = 14, Question = "Em qual ou quais características o aluno se encaixa?", AnswerList = _evaluation.RegularTeacherQuestionnaire.BehaviorTraits.Select(temp => temp.BehaviorTrait.GetDisplayName()).ToList(), Observations = _evaluation.RegularTeacherQuestionnaire.BehaviorObservations },
                    new() { Order = 15, Question = "Comparado aos outros integrantes da turma o aluno é:", Answer = _evaluation.RegularTeacherQuestionnaire.DevelopmentLevel.GetDisplayName(), Observations = _evaluation.RegularTeacherQuestionnaire.DevelopmentObservations },
                    new() { Order = 16, Question = "Relate qualquer informação que não tenha sido abordada ou que julgue importantes.", Answer = _evaluation.RegularTeacherQuestionnaire.AdditionalComments },
                };

                var screeningSections = new List<ScreeningSection>
                {
                    new ScreeningSection
                    {
                        Title = "Linguagem (Compreensão e Expressão Verbal)",
                        Questions = new List<ScreeningQuestion>
                        {
                            new ScreeningQuestion {
                                Question = "Usa frases muito simples ou vocabulário limitado para a idade?",
                                Answer = _evaluation.RegularTeacherScreening.LimitedVocabulary.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.LimitedVocabularyObservation
                            },
                            new ScreeningQuestion {
                                Question = "Apresenta fala infantilizada ou incompreensível?",
                                Answer = _evaluation.RegularTeacherScreening.ChildlikeSpeech.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.ChildlikeSpeechObservation
                            },
                            new ScreeningQuestion {
                                Question = "Tem dificuldade em seguir comandos simples?",
                                Answer = _evaluation.RegularTeacherScreening.DifficultyFollowingCommands.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.DifficultyFollowingCommandsObservation
                            },
                            new ScreeningQuestion {
                                Question = "Demora para responder ou parece não entender o que é dito?",
                                Answer = _evaluation.RegularTeacherScreening.DelayedResponse.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.DelayedResponseObservation
                            },
                            new ScreeningQuestion {
                                Question = "Evita conversar ou interagir verbalmente com colegas?",
                                Answer = _evaluation.RegularTeacherScreening.AvoidsTalking.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.AvoidsTalkingObservation
                            },
                        }
                    },
                    new ScreeningSection
                    {
                        Title = "Escrita",
                        Questions = new List<ScreeningQuestion>()
                        {
                            new ScreeningQuestion {
                                Question = "Apresenta dificuldade para copiar palavras/frases da lousa?",
                                Answer = _evaluation.RegularTeacherScreening.DifficultyCopying.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.DifficultyCopyingObservation
                            },
                            new ScreeningQuestion {
                                Question = "Comete erros ortográficos acima do esperado para a idade?",
                                Answer = _evaluation.RegularTeacherScreening.ExcessiveSpellingErrors.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.ExcessiveSpellingErrorsObservation
                            },
                            new ScreeningQuestion {
                                Question = "Mostra escrita desorganizada, ilegível ou muito lenta?",
                                Answer = _evaluation.RegularTeacherScreening.WritingIssues.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.WritingIssuesObservation
                            },
                            new ScreeningQuestion {
                                Question = "Dificuldade em associar sons às letras?",
                                Answer = _evaluation.RegularTeacherScreening.PhoneticAssociationDifficulty.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.PhoneticAssociationDifficultyObservation
                            },
                            new ScreeningQuestion {
                                Question = "Nível de escrita",
                                Answer = _evaluation.RegularTeacherScreening.WritingLevel.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.WritingLevelObservation
                            },
                        }
                    },
                    new ScreeningSection
                    {
                        Title = "Leitura",
                        Questions = new List<ScreeningQuestion>()
                        {
                            new ScreeningQuestion {
                                Question = "Reconhece letras ou palavras já trabalhadas?",
                                Answer = _evaluation.RegularTeacherScreening.RecognizesLettersOrWords.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.RecognizesLettersOrWordsObservation
                            },
                            new ScreeningQuestion {
                                Question = "Leitura silabada, lenta e sem compreensão?",
                                Answer = _evaluation.RegularTeacherScreening.ReadingFluencyIssues.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.ReadingFluencyIssuesObservation
                            },
                            new ScreeningQuestion {
                                Question = "Responde perguntas sobre o que leu?",
                                Answer = _evaluation.RegularTeacherScreening.AnswersReadingQuestions.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.AnswersReadingQuestionsObservation
                            },
                            new ScreeningQuestion {
                                Question = "Evita atividades de leitura?",
                                Answer = _evaluation.RegularTeacherScreening.AvoidsReading.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.AvoidsReadingObservation
                            },
                        }
                    },
                    new ScreeningSection
                    {
                        Title = "Raciocínio Lógico e Habilidades Numéricas",
                        Questions = new List<ScreeningQuestion>()
                        {
                            new ScreeningQuestion {
                                Question = "Reconhece números ou erra a contagem?",
                                Answer = _evaluation.RegularTeacherScreening.NumberRecognitionIssues.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.NumberRecognitionIssuesObservation
                            },
                            new ScreeningQuestion {
                                Question = "Compreende conceitos como maior/menor, primeiro/último?",
                                Answer = _evaluation.RegularTeacherScreening.ComparisonConceptDifficulty.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.ComparisonConceptDifficultyObservation
                            },
                            new ScreeningQuestion {
                                Question = "Dificuldade com operações básicas mesmo com apoio visual?",
                                Answer = _evaluation.RegularTeacherScreening.BasicOperationDifficulty.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.BasicOperationDifficultyObservation
                            },
                            new ScreeningQuestion {
                                Question = "Entende noções de tempo ou espaço (ontem/hoje, longe/perto)?",
                                Answer = _evaluation.RegularTeacherScreening.TimeSpaceConceptDifficulty.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.TimeSpaceConceptDifficultyObservation
                            },
                        }
                    },
                    new ScreeningSection
                    {
                        Title = "Autonomia / Funcionamento Adaptativo",
                        Questions = new List<ScreeningQuestion>()
                        {
                            new ScreeningQuestion {
                                Question = "Dificuldade para organizar seus materiais?",
                                Answer = _evaluation.RegularTeacherScreening.DifficultyOrganizingMaterials.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.DifficultyOrganizingMaterialsObservation
                            },
                            new ScreeningQuestion {
                                Question = "Necessita ajuda para atividades básicas (ir ao banheiro, recortar)?",
                                Answer = _evaluation.RegularTeacherScreening.NeedsHelpWithBasicActivities.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.NeedsHelpWithBasicActivitiesObservation
                            },
                            new ScreeningQuestion {
                                Question = "Dificuldade para seguir rotinas da sala?",
                                Answer = _evaluation.RegularTeacherScreening.DifficultyWithRoutines.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.DifficultyWithRoutinesObservation
                            },
                            new ScreeningQuestion {
                                Question = "Não inicia tarefas sozinho, mesmo após instruções?",
                                Answer = _evaluation.RegularTeacherScreening.DoesNotInitiateTasks.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.DoesNotInitiateTasksObservation
                            },
                        }
                    },
                    new ScreeningSection
                    {
                        Title = "Sincronia entre Idade Mental e Cronológica",
                        Questions = new List<ScreeningQuestion>()
                        {
                            new ScreeningQuestion {
                                Question = "Comportamentos infantis ou imaturos em comparação com a turma?",
                                Answer = _evaluation.RegularTeacherScreening.ImmatureBehavior.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.ImmatureBehaviorObservation
                            },
                            new ScreeningQuestion {
                                Question = "Prefere brincar com crianças mais novas?",
                                Answer = _evaluation.RegularTeacherScreening.PrefersYoungerChildren.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.PrefersYoungerChildrenObservation
                            },
                            new ScreeningQuestion {
                                Question = "Desempenho geral parece compatível com faixa etária inferior?",
                                Answer = _evaluation.RegularTeacherScreening.BelowAgeLevelPerformance.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.BelowAgeLevelPerformanceObservation
                            },
                            new ScreeningQuestion {
                                Question = "Habilidades semelhantes às de crianças da educação infantil?",
                                Answer = _evaluation.RegularTeacherScreening.EarlyChildhoodSkillLevel.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.EarlyChildhoodSkillLevelObservation
                            },
                        }
                    },
                    new ScreeningSection
                    {
                        Title = "Memória e Atenção",
                        Questions = new List<ScreeningQuestion>()
                        {
                            new ScreeningQuestion {
                                Question = "Esquece o que foi ensinado no dia anterior?",
                                Answer = _evaluation.RegularTeacherScreening.ForgetsContent.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.ForgetsContentObservation
                            },
                            new ScreeningQuestion {
                                Question = "Dificuldade de manter a atenção nas atividades?",
                                Answer = _evaluation.RegularTeacherScreening.AttentionDifficulty.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.AttentionDifficultyObservation
                            },
                            new ScreeningQuestion {
                                Question = "Distração fácil com estímulos externos?",
                                Answer = _evaluation.RegularTeacherScreening.EasilyDistracted.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.EasilyDistractedObservation
                            },
                            new ScreeningQuestion {
                                Question = "Requer explicações repetidas para entender?",
                                Answer = _evaluation.RegularTeacherScreening.NeedsRepeatedExplanations.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.NeedsRepeatedExplanationsObservation
                            },
                        }
                    },
                    new ScreeningSection
                    {
                        Title = "Interação Social e Emocional",
                        Questions = new List<ScreeningQuestion>()
                        {
                            new ScreeningQuestion {
                                Question = "Dificuldade para fazer ou manter amizades?",
                                Answer = _evaluation.RegularTeacherScreening.FriendshipDifficulty.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.FriendshipDifficultyObservation
                            },
                            new ScreeningQuestion {
                                Question = "Frustra-se com facilidade; pode reagir com agressividade?",
                                Answer = _evaluation.RegularTeacherScreening.EasilyFrustrated.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.EasilyFrustratedObservation
                            },
                            new ScreeningQuestion {
                                Question = "Demonstra baixa autoestima ou medo de errar?",
                                Answer = _evaluation.RegularTeacherScreening.LowSelfEsteem.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.LowSelfEsteemObservation
                            },
                            new ScreeningQuestion {
                                Question = "Depende de adultos para resolver conflitos simples?",
                                Answer = _evaluation.RegularTeacherScreening.DependsOnAdults.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.DependsOnAdultsObservation
                            },
                        }
                    },
                    new ScreeningSection
                    {
                        Title = "Histórico Educacional e Funcional",
                        Questions = new List<ScreeningQuestion>()
                        {
                            new ScreeningQuestion {
                                Question = "Apresenta histórico de baixo desempenho escolar desde os primeiros anos?",
                                Answer = _evaluation.RegularTeacherScreening.LowAcademicHistory.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.LowAcademicHistoryObservation
                            },
                            new ScreeningQuestion {
                                Question = "Recebeu apoio pedagógico sem evolução significativa?",
                                Answer = _evaluation.RegularTeacherScreening.NoProgressWithSupport.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.NoProgressWithSupportObservation
                            },
                            new ScreeningQuestion {
                                Question = "Há registros de pareceres ou relatórios anteriores que apontam comprometimento intelectual?",
                                Answer = _evaluation.RegularTeacherScreening.PreviousReportsIndicateImpairment.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.PreviousReportsIndicateImpairmentObservation
                            },
                            new ScreeningQuestion {
                                Question = "A escola já realizou adaptações curriculares para esse aluno?",
                                Answer = _evaluation.RegularTeacherScreening.CurricularAdaptations.GetDisplayName(),
                                Observation = _evaluation.RegularTeacherScreening.CurricularAdaptationsObservation
                            },
                        }
                    },
                    new ScreeningSection {
                        AdditionalText = _evaluation.RegularTeacherScreening.AdditionalComments
                    }
                };

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
                                .Text("Estudo de Caso")
                                .Style(PdfTheme.TitleStyle);

                            //col.Item()
                            //    .PaddingTop(4)
                            //    .AlignCenter()
                            //    .Text("Avaliação Psicopedagógica - Professor Regular")
                            //    .FontSize(10)
                            //    .Style(PdfTheme.MutedStyle);

                            col.Item().PaddingTop(48);
                            col.Item().Element(x => PdfTheme.Hr(x, 2));
                        });
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
                                                PdfTheme.Field(t, "Nome: ", _evaluation.Student?.FullName));

                                            row.RelativeItem().Text(t =>
                                            {
                                                var dob = _evaluation.Student?.DateOfBirth;
                                                var dobText = dob.HasValue ? dob.Value.ToString("dd/MM/yyyy") : "—";
                                                PdfTheme.Field(t, "Data de Nascimento: ", dobText);
                                            });
                                        });

                                        card.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text(t =>
                                                PdfTheme.Field(t, "Estado: ", _evaluation.Student?.State));

                                            row.RelativeItem().Text(t =>
                                                PdfTheme.Field(t, "Município: ", _evaluation.Student?.City));
                                        });

                                        card.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text(t =>
                                                PdfTheme.Field(t, "Ano/Série: ", _evaluation.Student?.Serie.GetDisplayName()));

                                            row.RelativeItem().Text(t =>
                                                PdfTheme.Field(t, "Escola: ", _evaluation.Student?.School?.Name));
                                        });

                                        card.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text(t =>
                                                PdfTheme.Field(t, "Profissional: ", _evaluation?.User?.FullName));
                                        });
                                    });
                            });

                            x.Item().PaddingBottom(16);

                            foreach (var item in items)
                            {
                                x.Item().Column(inner =>
                                {
                                    inner.Item().Row(row =>
                                    {
                                        row.ConstantItem(30)
                                            .Text($"{item.Order}.")
                                            .Style(PdfTheme.QuestionNumber);

                                        row.RelativeItem()
                                            .Text(item.Question)
                                            .Style(PdfTheme.QuestionText);
                                    });

                                    var hasList = item.AnswerList is { Count: > 0 };

                                    if (hasList)
                                    {
                                        inner.Item()
                                             .PaddingLeft(30)
                                             .Column(list =>
                                        {
                                            list.Spacing(4);

                                            foreach (var text in item.AnswerList!)
                                            {
                                                list.Item().Row(r =>
                                                {
                                                    r.ConstantItem(12).Text("•").Style(PdfTheme.Bullet);
                                                    r.RelativeItem().Text(text).Style(PdfTheme.AnswerText).Justify();
                                                });
                                            }

                                            if (!string.IsNullOrWhiteSpace(item.Observations))
                                            {
                                                list.Item().PaddingTop(2).PaddingBottom(12).Text($"Observações: {item.Observations}")
                                                    .Style(PdfTheme.Observation).Justify();
                                            }
                                        });
                                    }
                                    else
                                    {
                                        inner.Item()
                                            .PaddingLeft(30)
                                            .PaddingBottom(12)
                                            .Text(item.Answer ?? "—")
                                            .Style(PdfTheme.AnswerText).Justify();

                                        if (!string.IsNullOrWhiteSpace(item.Observations))
                                        {
                                            inner.Item()
                                                .PaddingLeft(30)
                                                .PaddingTop(2)
                                                .Text($"Observações: {item.Observations}")
                                                .Style(PdfTheme.Observation).Justify();
                                        }
                                    }
                                });
                            }

                            foreach (var section in screeningSections)
                            {
                                var hasTitle = !string.IsNullOrWhiteSpace(section.Title);
                                var hasAdditional = !string.IsNullOrWhiteSpace(section.AdditionalText);

                                if (!hasTitle && !hasAdditional)
                                    continue;

                                if (hasTitle)
                                {
                                    x.Item()
                                        .PaddingTop(16)
                                        .Text(section.Title!)
                                        .Style(PdfTheme.H2);
                                }
                                else if (hasAdditional)
                                {
                                    x.Item()
                                        .PaddingTop(16)
                                        .Text("Comentários Adicionais")
                                        .Style(PdfTheme.H2);
                                }

                                if (section.Questions is { Count: > 0 })
                                {
                                    foreach (var q in section.Questions)
                                    {
                                        x.Item().Row(row =>
                                        {
                                            row.ConstantItem(30)
                                                .Text("•")
                                                .Style(PdfTheme.Bullet);

                                            row.RelativeItem()
                                                .Text(q.Question)
                                                .Style(PdfTheme.QuestionText);
                                        });

                                        x.Item()
                                            .PaddingLeft(30)
                                            .PaddingTop(2)
                                            .Text(t =>
                                            {
                                                t.Span(string.IsNullOrWhiteSpace(q.Answer) ? "—" : q.Answer)
                                                 .Style(PdfTheme.AnswerText);
                                            });

                                        if (!string.IsNullOrWhiteSpace(q.Observation))
                                        {
                                            x.Item()
                                                .PaddingLeft(30)
                                                .PaddingTop(2)
                                                .Text(t =>
                                                {
                                                    t.Span("Observação: ").Style(PdfTheme.Label);
                                                    t.Span(q.Observation).Style(PdfTheme.Observation);
                                                });
                                        }

                                        x.Spacing(8);
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(section.AdditionalText))
                                {
                                    x.Item()
                                        .PaddingTop(6)
                                        .Text(section.AdditionalText)
                                        .Justify()
                                        .Style(PdfTheme.AnswerText);

                                    x.Spacing(8);
                                }
                            }
                        });


                    page.Footer()
                        .Element(f =>
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
           
        }
    }
    public class QuestionAnswer
    {
        public int Order { get; set; }
        public string Question { get; set; } = "";
        public string? Answer { get; set; }
        public List<string>? AnswerList { get; set; }
        public string? Observations { get; set; }
    }

    public class ScreeningSection
    {
        public string Title { get; set; } = "";
        public List<ScreeningQuestion> Questions { get; set; } = new();
        public string? AdditionalText { get; set; }
    }

    public class ScreeningQuestion
    {
        public int Order { get; set; }
        public string Question { get; set; } = "";
        public string Answer { get; set; }
        public string? Observation { get; set; }
    }

}
