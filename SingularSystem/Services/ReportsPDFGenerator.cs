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
    public class ReportsPDFGenerator : IDocumentPdfGenerator
    {
        public string Discriminator => "Report";

        public Task<byte[]> GenerateAsync(BaseDocument document, CancellationToken ct = default)
        {
            return GenerateAsync(document, PdfRenderContext.Default(PdfProfile.View), ct);
        }

        public Task<byte[]> GenerateAsync(BaseDocument document, PdfRenderContext context, CancellationToken ct = default)
        {
            var report = (Report)document;
            var bytes = new ReportPDFDocument(report, context).GeneratePdf();
            return Task.FromResult(bytes);
        }

        public static string FormatOther(
            string? value,
            string? otherValue,
            string[]? otherLabels = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            otherLabels ??= new[] { "Outro", "Outra", "Outros", "Outras" };

            if (otherLabels.Contains(value) && !string.IsNullOrWhiteSpace(otherValue))
                return otherValue.ToLowerInvariant();

            return value.ToLowerInvariant();
        }

        public static string FormatList(
            IEnumerable<string>? items,
            string? otherValue,
            string[]? otherLabels = null)
        {
            if (items == null)
                return string.Empty;

            otherLabels ??= new[] { "Outro", "Outra", "Outros", "Outras" };

            var mapped = items
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(item =>
                    otherLabels.Any(l => string.Equals(l, item, StringComparison.OrdinalIgnoreCase))
                    && !string.IsNullOrWhiteSpace(otherValue)
                        ? $"{item}: {otherValue}"
                        : item.ToLowerInvariant()
                )
                .ToList();

            if (mapped.Count == 0)
                return string.Empty;

            if (mapped.Count == 1)
                return mapped[0];

            if (mapped.Count == 2)
                return $"{mapped[0]} e {mapped[1]}";

            return $"{string.Join(", ", mapped.Take(mapped.Count - 1))} e {mapped.Last()}";
        }

        public static string FormatHealthConditions(
            IEnumerable<string>? conditions,
            string? otherCondition)
        {
            var list = conditions?.ToList() ?? new List<string>();

            static bool IsOther(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return false;

                return value.Trim().Equals("Outro", StringComparison.OrdinalIgnoreCase)
                    || value.Trim().Equals("Outra", StringComparison.OrdinalIgnoreCase)
                    || value.Trim().Equals("Outros", StringComparison.OrdinalIgnoreCase)
                    || value.Trim().Equals("Outras", StringComparison.OrdinalIgnoreCase);
            }

            if (list.Count == 0 || list.Any(c => string.Equals(c?.Trim(), "Não", StringComparison.OrdinalIgnoreCase)))
            {
                return "O(a) aluno(a) não apresenta condições de saúde significativas.";
            }

            var mapped = list
                .Where(c => !IsOther(c) && !string.Equals(c?.Trim(), "Não apresenta", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.ToLowerInvariant())
                .ToList();

            if (list.Any(IsOther) && !string.IsNullOrWhiteSpace(otherCondition))
            {
                mapped.Add(otherCondition.ToLowerInvariant());
            }

            if (mapped.Count == 0)
            {
                return "O(a) aluno(a) apresenta condições de saúde.";
            }

            string formatted =
                mapped.Count == 1
                    ? mapped[0]
                    : mapped.Count == 2
                        ? $"{mapped[0]} e {mapped[1]}"
                        : $"{string.Join(", ", mapped.Take(mapped.Count - 1))} e {mapped.Last()}";

            return $"O(a) aluno(a) apresenta condições de saúde, como {formatted}.";
        }

        public static string FormatContinuousMedication(
            string? choice,
            string? details,
            string? other)
        {
            if (string.IsNullOrWhiteSpace(choice) || choice == "Não")
            {
                return "Quanto à medicação, o(a) aluno(a) não faz uso de medicação contínua.";
            }

            if (choice == "Sim" && !string.IsNullOrWhiteSpace(details))
            {
                return $"Quanto à medicação, o(a) aluno(a) faz uso de medicação contínua, sendo: {details.ToLowerInvariant()}.";
            }

            if (choice == "Outra" && !string.IsNullOrWhiteSpace(other))
            {
                return $"Quanto à medicação, o(a) aluno(a) faz uso de medicação contínua, sendo: {other.ToLowerInvariant()}.";
            }

            return "Quanto à medicação, há registro de uso de medicação contínua.";
        }

        public static string GenerateCognitiveText(AdaptiveBehaviorScaleDI? scale)
        {
            if (scale is null)
                return string.Empty;

            var text = string.Empty;
            
            text +=
                $"Na avaliação do comportamento adaptativo, o domínio conceitual apresentou {scale.Percentage}% de funcionamento";

            if (scale.Percentage < 51)
            {
                text +=
                    ", indicando necessidade de suporte significativo em habilidades conceituais como linguagem, leitura, escrita e raciocínio.";
            }
            else if (scale.Percentage < 75)
            {
                text +=
                    ", sugerindo necessidade de suporte moderado para o desenvolvimento de habilidades conceituais.";
            }
            else
            {
                text +=
                    ", demonstrando bom desenvolvimento das habilidades conceituais.";
            }

            return text;
        }

        public static string GetEmotionalDevelopment(int percentage)
        {
            var text = string.Empty;

            if (percentage < 25)
            {
                text +=
                    "Indicadores emocionais dentro do esperado. Manter estímulos socioemocionais e acompanhamento escolar.";
            }

            if (percentage < 50)
            {
                text +=
                    "Alguns sinais de dificuldade emocional. Recomenda-se monitoramento e estratégias de apoio socioemocional.";
            }

            if (percentage < 70)
            {
                text +=
                    "Sinais relevantes de dificuldade emocional. Recomenda-se avaliação mais detalhada e plano de intervenção.";
            }

            text +=
                "Sinais importantes de dificuldade emocional. Encaminhamento e acompanhamento especializado são recomendados.";

            return text;
        }

        public sealed record EmotionalPercentageResult(int Total, int Max, int Percentage);

        public static class PsychologistQuestionnaireScoring
        {
            private static int GetEvaluationScore(Evaluation e) => e switch
            {
                Evaluation.Yes => 2,
                Evaluation.Sometimes => 1,
                Evaluation.Not => 0,
                Evaluation.None => 0,
                _ => 0
            };

            public static EmotionalPercentageResult CalculateEmotionalPercentage(PsychologistQuestionnaire? q)
            {
                if (q is null)
                    return new EmotionalPercentageResult(0, 0, 0);

                // Equivalente ao EMOTIONAL_FIELDS do TS
                var answers = new List<Evaluation>
        {
            q.FriendshipDifficulty,
            q.EasilyFrustrated,
            q.LowSelfEsteem,
            q.DependsOnAdults
        };
                var valid = answers.Where(a => a != Evaluation.None).ToList();

                var total = valid.Sum(GetEvaluationScore);
                var max = valid.Count * 2;

                var percentage = max > 0
                    ? (int)Math.Round((double)total / max * 100.0, MidpointRounding.AwayFromZero)
                    : 0;

                return new EmotionalPercentageResult(total, max, percentage);
            }
        }

        public static string GenerateSocialText(AdaptiveBehaviorScaleDI? scale)
        {
            if (scale is null)
                return string.Empty;

            var text = string.Empty;

            text +=
                $"No domínio social do comportamento adaptativo, observou-se {scale.PercentageSocialSkills}% de funcionamento";

            if (scale.PercentageSocialSkills < 51)
            {
                text +=
                    ", indicando dificuldades significativas em habilidades sociais e interpessoais.";
            }
            else if (scale.PercentageSocialSkills < 75)
            {
                text +=
                    ", sugerindo necessidade de apoio para desenvolvimento de competências sociais.";
            }
            else
            {
                text +=
                    ", demonstrando adequado desenvolvimento de habilidades sociais.";
            }
 

            return text;
        }

        public static List<string> GenerateRecommendationsText(PsychologistReport? relatorio)
        {
            var recommendations = new List<string>();

            if (
                relatorio?.SnapIVQuestionnaire?.Result != null &&
                (
                    relatorio.SnapIVQuestionnaire.Result == SnapIVResult.Inattentive ||
                    relatorio.SnapIVQuestionnaire.Result == SnapIVResult.HyperactiveImpulsive ||
                    relatorio.SnapIVQuestionnaire.Result == SnapIVResult.Combined
                )
            )
            {
                recommendations.Add(
                    "Encaminhamento para avaliação médica neurológica ou psiquiátrica para confirmação diagnóstica de TDAH"
                );
                recommendations.Add(
                    "Adaptações pedagógicas para favorecer a atenção e concentração"
                );
                recommendations.Add(
                    "Estabelecimento de rotinas estruturadas"
                );
            }

            if (
                relatorio?.AdaptiveBehaviorScaleDI?.Result != null &&
                relatorio.AdaptiveBehaviorScaleDI.Result != ScaleDIResult.NoDevelopmentalDelay &&
                relatorio.AdaptiveBehaviorScaleDI.Result != ScaleDIResult.None
            )
            {
                recommendations.Add(
                    "Acompanhamento psicopedagógico especializado"
                );
                recommendations.Add(
                    "Desenvolvimento de programa individualizado de ensino (PEI)"
                );
                recommendations.Add(
                    "Parceria família-escola para estimulação de habilidades adaptativas"
                );
            }

            recommendations.Add(
                "Reavaliação periódica para acompanhamento da evolução"
            );

            return recommendations;
        }

        public static string GeneratePsychologistAutoConclusion(PsychologistReport? relatorio)
        {
            var text =
                "Com base nos instrumentos aplicados e nas observações realizadas, conclui-se que:\n\n";

            if (relatorio?.SnapIVQuestionnaire?.Result != null)
            {
                var result = relatorio.SnapIVQuestionnaire.Result;

                if (
                    result == SnapIVResult.Combined ||
                    result == SnapIVResult.Inattentive ||
                    result == SnapIVResult.HyperactiveImpulsive
                )
                {
                    text +=
                        "Os resultados da Escala SNAP-IV são SUGESTIVOS DE TDAH (Transtorno de Déficit de Atenção e Hiperatividade). ";

                    if (result == SnapIVResult.Combined)
                    {
                        text +=
                            "Os sintomas de desatenção e hiperatividade/impulsividade estão presentes de forma significativa. ";
                    }
                    else if (result == SnapIVResult.Inattentive)
                    {
                        text +=
                            "Predominam sintomas de desatenção. ";
                    }
                    else
                    {
                        text +=
                            "Predominam sintomas de hiperatividade/impulsividade. ";
                    }

                    text +=
                        "Recomenda-se avaliação médica especializada para diagnóstico definitivo.\n\n";
                }
                else
                {
                    text +=
                        "Os resultados da Escala SNAP-IV NÃO são sugestivos de TDAH no momento da avaliação.\n\n";
                }
            }

            if (
                relatorio?.AdaptiveBehaviorScaleDI?.Result != null &&
                relatorio.AdaptiveBehaviorScaleDI.Result != ScaleDIResult.NoDevelopmentalDelay &&
                relatorio.AdaptiveBehaviorScaleDI.Result != ScaleDIResult.None
            )
            {
                text +=
                    "A avaliação indica SUGESTIVO DE LIMITAÇÃO NO COMPORTAMENTO ADAPTATIVO, com prejuízos significativos em pelo menos duas áreas do funcionamento adaptativo. ";

                text +=
                    "Isso sugere necessidade de suporte especializado para o desenvolvimento de habilidades práticas do dia a dia.\n\n";
            }

            return text;
        }

        public static string GeneratePsychopedagogueAutoConclusion(PsychopedagogueReport? relatorio)
        {
            if (relatorio == null) return string.Empty;

            var text = string.Empty;

            if (relatorio.CARSQuestionnaire != null)
            {
                var result = relatorio.CARSQuestionnaire.Result;
                if (result != CARSResult.NoAutism && result != CARSResult.None)
                {
                    text += "Os resultados da Escala CARS são SUGESTIVOS DE SINTOMAS DE TRANSTORNO DO ESPECTRO AUTISTA (TEA). Recomenda-se encaminhamento para avaliação neuropediátrica ou psiquiátrica especializada.\n\n";
                }
                else if (result == CARSResult.NoAutism)
                {
                    text += "Os resultados da Escala CARS NÃO indicam sintomas sugestivos de Transtorno do Espectro Autista (TEA) no momento da avaliação.\n\n";
                }
            }
            else if (relatorio.MCHATQuestionnaire != null)
            {
                var result = relatorio.MCHATQuestionnaire.Result;
                if (result != MCHATResult.LowRisk && result != MCHATResult.None)
                {
                    text += "Os resultados da Escala M-CHAT são SUGESTIVOS DE RISCO PARA TRANSTORNO DO ESPECTRO AUTISTA (TEA). Recomenda-se encaminhamento médico para acompanhamento de desenvolvimento.\n\n";
                }
                else if (result == MCHATResult.LowRisk)
                {
                    text += "Os resultados da Escala M-CHAT indicam BAIXO RISCO para Transtorno do Espectro Autista (TEA).\n\n";
                }
            }

            if (relatorio.CognitiveAssessment != null)
            {
                var pct = relatorio.CognitiveAssessment.Percentage;
                var result = relatorio.CognitiveAssessment.Result;
                if (pct <= 55 || (result != CognitiveResult.WithinExpected && result != CognitiveResult.None))
                {
                    text += "A Avaliação do Funcionamento Intelectual indica NECESSIDADE DE SUPORTE NO DESENVOLVIMENTO COGNITIVO. Recomenda-se acompanhamento psicopedagógico especializado e adaptação curricular.\n\n";
                }
                else
                {
                    text += "A Avaliação do Funcionamento Intelectual indica DESENVOLVIMENTO COGNITIVO DENTRO DO ESPERADO para a faixa etária.\n\n";
                }
            }

            return text;
        }

        private static string GenerateSocialAutoConclusion(SocialWorkerReport? socialWorkerReport, string? studentName)
        {
            if (socialWorkerReport == null) return string.Empty;

            var income = socialWorkerReport.MonthlyHouseholdIncome;
            var foodSecurity = socialWorkerReport.FoodSecurity;
            var housing = socialWorkerReport.HousingConditions;
            var typeOfHousing = socialWorkerReport.TypeOfHousing;
            var dailyMeals = socialWorkerReport.DailyMeals;

            var otherIncome = socialWorkerReport.OtherMonthlyHouseholdIncome?.ToLowerInvariant() ?? "";
            var otherFood = socialWorkerReport.OtherFoodSecurity?.ToLowerInvariant() ?? "";
            var otherHousing = socialWorkerReport.OtherHousingConditions?.ToLowerInvariant() ?? "";
            var otherTypeOfHousing = socialWorkerReport.OtherTypeOfHousing?.ToLowerInvariant() ?? "";
            var otherIncomeSource = socialWorkerReport.OtherFamilyIncomeSource?.ToLowerInvariant() ?? "";

            var incomeSources = socialWorkerReport.FamilyIncomeSource?.Select(s => s.Source) ?? Enumerable.Empty<IncomeSource>();
            var benefits = socialWorkerReport.ReceivedGovernmentBenefits?.Select(b => b.Benefit) ?? Enumerable.Empty<GovernmentBenefits>();

            // 1. Fatores Críticos de Vulnerabilidade (Sempre indicam desafios)
            var hasCriticalChallenge = income == MonthlyFamilyIncome.UpToOneMinimumWage ||
                                     incomeSources.Any(s => s == IncomeSource.Unemployed) ||
                                     housing == HousingCondition.Precarious ||
                                     housing == HousingCondition.Unhealthy ||
                                     foodSecurity == FoodSecurity.FoodInsecurity ||
                                     dailyMeals == DailyMeals.OneOrLess ||
                                     otherHousing.Contains("sem banheiro") ||
                                     otherHousing.Contains("não tem banheiro") ||
                                     otherHousing.Contains("risco") ||
                                     otherFood.Contains("insegurança") ||
                                     otherFood.Contains("falta");

            // 2. Fatores de Risco (Indicam desafios se não houver estabilidade)
            var hasRiskFactors = income == MonthlyFamilyIncome.FromOneToTwoMinimumWages ||
                                incomeSources.Any(s => s == IncomeSource.InformalEmployment || s == IncomeSource.SocialBenefits) ||
                                benefits.Any(b => b != GovernmentBenefits.None) ||
                                typeOfHousing == HousingType.ProvidedByFamilyOrOthers ||
                                dailyMeals == DailyMeals.Two ||
                                otherIncomeSource.Contains("instável") ||
                                otherIncomeSource.Contains("bico") ||
                                otherIncomeSource.Contains("auxílio");

            // 3. Indicadores de Estabilidade (Podem compensar fatores de risco)
            var hasStabilityFactors = income == MonthlyFamilyIncome.FromTwoToThreeMinimumWages ||
                                     income == MonthlyFamilyIncome.AboveThreeMinimumWages ||
                                     incomeSources.Any(s => s == IncomeSource.FormalEmployment) ||
                                     housing == HousingCondition.Adequate;

            // Lógica Final: Desafio se tiver fator crítico OU (fator de risco SEM estabilidade)
            var hasSocioEconomicChallenges = hasCriticalChallenge || (hasRiskFactors && !hasStabilityFactors);

            string text;

            if (hasSocioEconomicChallenges)
            {
                text = $"Com base nas informações coletadas, observa-se que {studentName} está inserido(a) em um contexto familiar que apresenta desafios socioeconômicos que podem impactar seu desenvolvimento e aprendizagem. ";
            }
            else
            {
                text = $"Com base nas informações coletadas, observa-se que {studentName} está inserido(a) em um contexto familiar que não apresenta desafios socioeconômicos significativos no momento, o que favorece seu desenvolvimento. ";
            }

            text += "A família demonstra interesse no acompanhamento escolar, e o(a) aluno(a) apresenta potencial para desenvolvimento pleno, desde que receba o suporte adequado. ";

            if (hasSocioEconomicChallenges)
            {
                text += "É fundamental que sejam implementadas ações integradas entre escola, família e rede de proteção social para garantir os direitos e promover o bem-estar do(a) aluno(a).";
            }

            return text;
        }

        public class ReportPDFDocument : IDocument
        {
            private readonly Report _report;
            private readonly PdfRenderContext _context;

            public ReportPDFDocument(Report report, PdfRenderContext? context = null)
            {
                _report = report;
                _context = context ?? PdfRenderContext.Default(PdfProfile.View);
            }

            public DocumentMetadata GetMetadata()
            {
                var studentName = _report?.Student?.FullName ?? "Aluno";
                return PdfMetadataFactory.CreateMetadata(
                    _context.Profile,
                    $"Relatório Biopsicossocial - {studentName}");
            }

            public DocumentSettings GetSettings() => PdfMetadataFactory.CreateSettings(_context.Profile);

            public void Compose(IDocumentContainer container)
            {
                var culture = new CultureInfo("pt-BR");
                
                var snapIVResult = $"Resultado: Sugestivo de {_report?.PsychologistReport?.SnapIVQuestionnaire?.Result.GetDisplayName()}" ?? "";
                var adaptativeScore = $"Pontuação: {_report?.PsychologistReport?.AdaptiveBehaviorScaleDI?.Score} pontos" ?? "Pontuação: 0 pontos";
                var adaptativeResult = $"Resultado: Sugestivo de {_report?.PsychologistReport?.AdaptiveBehaviorScaleDI?.Result.GetDisplayName()}" ?? "";
                var carsScore = $"Pontuação: {_report?.PsychopedagogueReport?.CARSQuestionnaire?.TotalScore} pontos" ?? "Pontuação: 0 pontos";
                var carsResult = $"Resultado: Sugestivo de {_report?.PsychopedagogueReport?.CARSQuestionnaire?.Result.GetDisplayName()}" ?? "";
                var mchatScore = $"Pontuação: {_report?.PsychopedagogueReport?.MCHATQuestionnaire?.Score} pontos" ?? "Pontuação: 0 pontos";
                var mchatResult = $"Resultado: Sugestivo de {_report?.PsychopedagogueReport?.MCHATQuestionnaire?.Result.GetDisplayName()}" ?? "";
                var cognitiveScore = $"Pontuação: {_report?.PsychopedagogueReport?.CognitiveAssessment?.Score} pontos" ?? "Pontuação: 0 pontos";
                var cognitivePercentage = $"Percentual de Acertos: {_report?.PsychopedagogueReport?.CognitiveAssessment?.Percentage}%" ?? "Percentual de Acertos: 0%";
                var cognitiveResult = $"Resultado: Sugestivo de {_report?.PsychopedagogueReport?.CognitiveAssessment?.Result.GetDisplayName()}" ?? "";

                var dateOfBirth = _report?.Student?.DateOfBirth;
                var formattedDateOfBirth = dateOfBirth?.ToString("dd/MM/yyyy");
                var age = DateTime.Now.Year - dateOfBirth?.Year -
                         (DateTime.Now.DayOfYear < dateOfBirth?.DayOfYear ? 1 : 0);

                var socialIdentificacao = $"IDENTIFICAÇÃO: {_report?.Student?.FullName}, {age} anos, nascido(a) em {formattedDateOfBirth}, cursando o {_report?.Student?.Serie.GetDisplayName()} na {_report?.Student?.School?.Name}. Responsável: {_report?.Student?.NameOfPersonResponsible}.";
                var socialComposicaoFamiliar = $"COMPOSIÇÃO FAMILIAR: A família apresenta composição {FormatOther(_report?.SocialWorkerReport?.FamilyComposition.GetDisplayName(), _report?.SocialWorkerReport?.OtherFamilyComposition)}. A residência é composta por {FormatOther(_report?.SocialWorkerReport?.NumberOfPeopleInTheHousehold.GetDisplayName(), _report?.SocialWorkerReport?.OtherNumberOfPeopleInTheHousehold)}.";
                var socialObservacoesMoradores = !string.IsNullOrWhiteSpace(_report?.SocialWorkerReport?.AdditionalObservationHouseholdMembers)
                    ? $"OBSERVAÇÕES SOBRE MORADORES: {_report!.SocialWorkerReport!.AdditionalObservationHouseholdMembers}"
                    : string.Empty;
                var socialMoradia = $"MORADIA: A família reside em casa {FormatOther(_report?.SocialWorkerReport?.TypeOfHousing.GetDisplayName(), _report?.SocialWorkerReport?.OtherTypeOfHousing)}, com condição considerada {FormatOther(_report?.SocialWorkerReport?.HousingConditions.GetDisplayName(), _report?.SocialWorkerReport?.OtherHousingConditions)}. A moradia possui {FormatOther(_report?.SocialWorkerReport?.NumberOfRooms.GetDisplayName(), _report?.SocialWorkerReport?.OtherNumberOfRooms)}.";
                var socialRendaFamiliar = $"RENDA FAMILIAR: A renda familiar mensal é de {FormatOther(_report?.SocialWorkerReport?.MonthlyHouseholdIncome.GetDisplayName(), _report?.SocialWorkerReport?.OtherMonthlyHouseholdIncome)}, proveniente de {FormatList(_report?.SocialWorkerReport?.FamilyIncomeSource?.Select(temp => temp.Source.GetDisplayName()), _report?.SocialWorkerReport?.OtherFamilyIncomeSource)}. Benefícios: {FormatList(_report?.SocialWorkerReport?.ReceivedGovernmentBenefits?.Select(temp => temp.Benefit.GetDisplayName()), _report?.SocialWorkerReport?.OtherReceivedGovernmentBenefits)}.";
                var socialCondicoesDeSaude = $"CONDIÇÕES DE SAÚDE: {FormatHealthConditions(_report?.SocialWorkerReport?.HealthConditions.Select(temp => temp.Condition.GetDisplayName()), _report?.SocialWorkerReport?.OtherDetailsHealthConditions)} {FormatContinuousMedication(_report?.SocialWorkerReport?.ContinuousMedication.GetDisplayName(), _report?.SocialWorkerReport?.DetailsContinuousMedication, _report?.SocialWorkerReport?.OtherContinuousMedication)} O acesso a serviços de saúde é {FormatOther(_report?.SocialWorkerReport?.HealthServiceAccess.GetDisplayName(), _report?.SocialWorkerReport?.OtherHealthServiceAccess)}.";
                var socialEducacao = $"EDUCAÇÃO: A frequência escolar é {FormatOther(_report?.SocialWorkerReport?.SchoolAttendance.GetDisplayName(), _report?.SocialWorkerReport?.OtherSchoolAttendance)}, e o desempenho acadêmico é considerado {FormatOther(_report?.SocialWorkerReport?.AcademicPerformance.GetDisplayName(), _report?.SocialWorkerReport?.OtherAcademicPerformance)}. Principais dificuldades: {FormatList(_report?.SocialWorkerReport?.MainSchoolDifficulties?.Select(temp => temp.Difficulty.GetDisplayName()), _report?.SocialWorkerReport?.OtherMainSchoolDifficulties)}.";
                var socialAlimentacao = $"ALIMENTAÇÃO: O(a) aluno(a) realiza {FormatOther(_report?.SocialWorkerReport?.DailyMeals.GetDisplayName(), _report?.SocialWorkerReport?.OtherDailyMeals)} diariamente. A situação de segurança alimentar é caracterizada como {FormatOther(_report?.SocialWorkerReport?.FoodSecurity.GetDisplayName(), _report?.SocialWorkerReport?.OtherFoodSecurity)}.";
                var socialConvivencia = $"CONVIVÊNCIA SOCIAL: O relacionamento familiar é descrito como {FormatOther(_report?.SocialWorkerReport?.FamilyRelationship.GetDisplayName(), _report?.SocialWorkerReport?.OtherFamilyRelationship )}. A rede de apoio social é {FormatOther(_report?.SocialWorkerReport?.SocialSupportNetwork.GetDisplayName(), _report?.SocialWorkerReport?.OtherSocialSupportNetwork)}. A participação em atividades comunitárias é {FormatOther(_report?.SocialWorkerReport?.CommunityActivityParticipation.GetDisplayName(), _report?.SocialWorkerReport?.OtherCommunityActivityParticipation)}.";
                var socialLines = new List<string>
                {
                    socialIdentificacao,
                    socialComposicaoFamiliar,
                    socialObservacoesMoradores,
                    socialMoradia,
                    socialRendaFamiliar,
                    socialCondicoesDeSaude,
                    socialEducacao,
                    socialAlimentacao,
                    socialConvivencia
                };
                var socialText = string.Join("\n", socialLines.Where(l => !string.IsNullOrWhiteSpace(l)));

                var desenvolvimentoCognitivo = $"{GenerateCognitiveText(_report?.PsychologistReport?.AdaptiveBehaviorScaleDI)}";
                var emotionalPercentage = PsychologistQuestionnaireScoring.CalculateEmotionalPercentage(_report?.PsychologistReport?.PsychologistQuestionnaire);
                var desenvolvimentoEmocional = $"{GetEmotionalDevelopment(emotionalPercentage.Percentage)}";
                var desenvolvimentoSocial = $"{GenerateSocialText(_report?.PsychologistReport?.AdaptiveBehaviorScaleDI)}";

                var autoSocialConclusion = GenerateSocialAutoConclusion(_report?.SocialWorkerReport, _report?.Student?.FullName);
                var autoPsychologistConclusion = GeneratePsychologistAutoConclusion(_report?.PsychologistReport);
                var autoPsychopedagogueAutoText = GeneratePsychopedagogueAutoConclusion(_report?.PsychopedagogueReport);
                var autoPsychopedagogueJustification = _report?.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.JustificationForClassification ?? string.Empty;

                var autoPsychopedagogueConclusion = string.IsNullOrWhiteSpace(autoPsychopedagogueAutoText)
                    ? autoPsychopedagogueJustification
                    : $"{autoPsychopedagogueAutoText}{autoPsychopedagogueJustification}";

                var manualConclusions = new List<string>();
                if (!string.IsNullOrWhiteSpace(_report?.SocialWorkerReport?.Conclusion))
                    manualConclusions.Add($"Observações do(a) Assistente Social:\n{_report.SocialWorkerReport.Conclusion}");
                
                if (!string.IsNullOrWhiteSpace(_report?.PsychopedagogueReport?.Conclusion))
                    manualConclusions.Add($"Observações do(a) Psicopedagogo(a):\n{_report.PsychopedagogueReport.Conclusion}");

                if (!string.IsNullOrWhiteSpace(_report?.PsychologistReport?.Conclusion))
                    manualConclusions.Add($"Observações do(a) Psicólogo(a):\n{_report.PsychologistReport.Conclusion}");

                var conclusao = $"{autoSocialConclusion}\n\n{autoPsychologistConclusion}\n\n{autoPsychopedagogueConclusion}";
                if (manualConclusions.Any())
                {
                    conclusao += "\n\n" + string.Join("\n\n", manualConclusions);
                }

                var checkedDisabilitiesList = _report?.PsychopedagogueReport?
                    .PsychopedagogueQuestionnaire?
                    .TypeOfDisability?
                    .Select(d => d.Disability.GetDisplayName())
                    .ToList() ?? new List<string>();

                var otherDisability = _report?.PsychopedagogueReport?
                    .PsychopedagogueQuestionnaire?
                    .OtherTypesOfDisability;

                if (!string.IsNullOrWhiteSpace(otherDisability))
                {
                    checkedDisabilitiesList.Add(otherDisability);
                }

                var disabilities = checkedDisabilitiesList.Any() 
                    ? string.Join(", ", checkedDisabilitiesList) 
                    : "-";

                var disorders = string.Join(
                        ", ",
                        _report?.PsychopedagogueReport?
                            .PsychopedagogueQuestionnaire?
                            .TypeOfDisorder?
                            .Select(d => d.Disorder.GetDisplayName())
                        ?? Enumerable.Empty<string>()
                    );

                var psychologistRecommendations = GenerateRecommendationsText(_report?.PsychologistReport);
                var socialWorkerRecommendations = _report?.SocialWorkerReport?.Recommendations ?? new List<SocialWorkerReport_Recommendation>();
                var socialWorkerOtherRecommendations = _report?.SocialWorkerReport?.OtherRecommendations;

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(_ => PdfTheme.BaseStyle);

                    page.Header().Element(h =>
                    {
                        PdfBrandChrome.ComposeHeader(h, _context, "Relatório Biopsicossocial");
                    });

                    page.Content()
                        .PaddingTop(8)
                        .PaddingBottom(16)
                        .Column(x =>
                        {
                            var student = _report?.Student;
                            var hasAnyOptional = !string.IsNullOrWhiteSpace(student?.State)
                                || !string.IsNullOrWhiteSpace(student?.City)
                                || !string.IsNullOrWhiteSpace(student?.School?.Name)
                                || student?.Serie != null;

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
                                                PdfTheme.Field(t, "Nome: ", student?.FullName));

                                            row.RelativeItem().Text(t =>
                                            {
                                                var dob = student?.DateOfBirth;
                                                var dobText = dob.HasValue ? dob.Value.ToString("dd/MM/yyyy") : "—";
                                                PdfTheme.Field(t, "Data de Nascimento: ", dobText);
                                            });
                                        });

                                        if (hasAnyOptional)
                                        {
                                            card.Item().Row(row =>
                                            {
                                                var hasState = !string.IsNullOrWhiteSpace(student?.State);
                                                var hasCity = !string.IsNullOrWhiteSpace(student?.City);

                                                if (hasState || hasCity)
                                                {
                                                    row.RelativeItem().Text(t =>
                                                    {
                                                        PdfTheme.FieldIfPresent(t, "Estado: ", student?.State);
                                                        if (hasState && hasCity) t.Span("   ");
                                                        PdfTheme.FieldIfPresent(t, "Município: ", student?.City);
                                                    });
                                                }

                                                var serie = student?.Serie != null ? student.Serie.GetDisplayName() : null;
                                                var school = student?.School?.Name;

                                                if (!string.IsNullOrWhiteSpace(serie) || !string.IsNullOrWhiteSpace(school))
                                                {
                                                    row.RelativeItem().Text(t =>
                                                    {
                                                        PdfTheme.FieldIfPresent(t, "Ano/Série: ", serie);
                                                        if (!string.IsNullOrWhiteSpace(serie) && !string.IsNullOrWhiteSpace(school)) t.Span("   ");
                                                        PdfTheme.FieldIfPresent(t, "Escola: ", school);
                                                    });
                                                }
                                            });
                                        }

                                        card.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text(t =>
                                                PdfTheme.Field(t, "Profissional: ", _report?.User?.FullName));
                                        });
                                    });
                            });

                            x.Item().PaddingBottom(16);

                            x.Item().PaddingBottom(4).Text("1. Contexto do Relatório").Style(PdfTheme.QuestionText);
                            x.Item().PaddingBottom(8).Text("Este relatório NÃO se trata de um laudo médico e nem tampouco substitui a avaliação de um profissional médico. Trata-se de uma triagem com intuito exclusivamente pedagógico e de fornecer informações para a melhoria acadêmica do estudante.").Style(PdfTheme.AnswerText).Justify();

                            x.Item().PaddingBottom(4).Text("2. Instrumentos Utilizados").Style(PdfTheme.QuestionText);
                            var instrumentosList = new List<string>();
                            instrumentosList.Add("ESCALA SNAP-IV: Instrumento de rastreamento para Transtorno de Déficit de Atenção e Hiperatividade (TDAH), composto por 18 itens que avaliam sintomas de desatenção e hiperatividade/impulsividade.");
                            if (_report?.PsychologistReport?.AdaptiveBehaviorScaleDI != null)
                            {
                                instrumentosList.Add("ESCALA ADAPTATIVA-DI: Avalia o funcionamento adaptativo do estudante em três domínios principais: habilidades conceituais, sociais e práticas, permitindo identificar o nível de independência e adaptação ao ambiente.");
                            }
                            instrumentosList.Add("QUESTIONÁRIO DE TRIAGEM MULTIPROFISSIONAL (Ficha de Triagem 1.1): Instrumento de triagem abrangente que avalia múltiplas áreas do desenvolvimento, incluindo linguagem, escrita, leitura, raciocínio lógico, autonomia, memória, atenção e interação socioemocional.");
                            if (_report?.PsychopedagogueReport?.MCHATQuestionnaire != null)
                            {
                                instrumentosList.Add("ESCALA M-CHAT (Modified Checklist for Autism in Toddlers): é um instrumento de rastreamento precoce para sinais do Transtorno do Espectro Autista (TEA), aplicado a crianças pequenas, especialmente entre 16 e 30 meses de idade.");
                            }
                            else
                            {
                                instrumentosList.Add("ESCALA CARS (Childhood Autism Rating Scale): é uma escala comportamental de 15 itens usada para avaliar a presença e a intensidade de características do Transtorno do Espectro Autista (TEA).");
                            }
                            if (_report?.PsychopedagogueReport?.CognitiveAssessment != null)
                            {
                                instrumentosList.Add("ESCALA de Funcionamento Intelectual: Instrumento composto por 10 itens que avalia o funcionamento cognitivo geral, considerando habilidades como raciocínio, memória, atenção, linguagem, aprendizagem e resolução de problemas.");
                            }
                            instrumentosList.Add("Questionário Psicopedagógico: Instrumento destinado à investigação do perfil de aprendizagem, avaliando habilidades acadêmicas, cognitivas, comportamentais e fatores que podem interferir no desempenho escolar.");
                            
                            var instrumentosText = string.Join("\r\n", instrumentosList);
                            x.Item().PaddingBottom(8).Text(instrumentosText).Style(PdfTheme.AnswerText).Justify();

                            x.Item().PaddingBottom(4).Text("3. Resultados").Style(PdfTheme.QuestionText);
                            x.Item().PaddingLeft(8).PaddingBottom(8).Text("3.1 Escala SNAP-IV").Style(PdfTheme.QuestionText).Justify();
                            x.Item().PaddingLeft(8).PaddingBottom(8).Text(snapIVResult).Style(PdfTheme.AnswerText).Justify();

                            x.Item().PaddingLeft(8).PaddingBottom(8).Text("3.2 Avaliação de TEA (Transtorno do Espectro Autista)").Style(PdfTheme.QuestionText).Justify();

                            if (_report?.PsychopedagogueReport?.CARSQuestionnaire != null) {
                                x.Item().PaddingLeft(8).PaddingBottom(8).Text(carsScore).Style(PdfTheme.AnswerText).Justify();
                                x.Item().PaddingLeft(8).PaddingBottom(8).Text(carsResult).Style(PdfTheme.AnswerText).Justify();
                            }else
                            {
                                x.Item().PaddingLeft(8).PaddingBottom(8).Text(mchatScore).Style(PdfTheme.AnswerText).Justify();
                                x.Item().PaddingLeft(8).PaddingBottom(8).Text(mchatResult).Style(PdfTheme.AnswerText).Justify();
                            }

                            if (_report?.PsychopedagogueReport?.CognitiveAssessment != null)
                            {
                                x.Item().PaddingLeft(8).PaddingBottom(8).Text("3.3 Avaliação de Funcionamento Intelectual").Style(PdfTheme.QuestionText).Justify();
                                x.Item().PaddingLeft(8).PaddingBottom(8).Text(cognitiveScore).Style(PdfTheme.AnswerText).Justify();
                                x.Item().PaddingLeft(8).PaddingBottom(8).Text(cognitivePercentage).Style(PdfTheme.AnswerText).Justify();
                                x.Item().PaddingLeft(8).PaddingBottom(8).Text(cognitiveResult).Style(PdfTheme.AnswerText).Justify();
                            }

                            x.Item().PaddingBottom(4).Text("4. Procedimentos Utilizados na Avaliação Social").Style(PdfTheme.QuestionText);
                            x.Item().PaddingBottom(2).Text("Para a elaboração deste relatório, foram utilizados os seguintes procedimentos:").Style(PdfTheme.AnswerText).Justify();
                            x.Item().PaddingBottom(8).Column(inner =>
                            {
                                inner.Item()
                                    .PaddingLeft(30)
                                    .Column(list =>
                                    {
                                        list.Spacing(4);

                                        foreach (var procedure in _report?.SocialWorkerReport?.ProceduresUsed ?? Enumerable.Empty<SocialWorkerReport_Procedure>())
                                        {
                                            list.Item().Row(r =>
                                            {
                                                r.ConstantItem(12).Text("•").Style(PdfTheme.Bullet);
                                                r.RelativeItem().Text(procedure.Procedure.GetDisplayName()).Style(PdfTheme.AnswerText).Justify();
                                            });
                                        }

                                        if (!string.IsNullOrWhiteSpace(_report?.SocialWorkerReport?.OtherProceduresUsed))
                                        {
                                            list.Item().PaddingTop(6).Text($"Outros procedimentos: {_report?.SocialWorkerReport?.OtherProceduresUsed}")
                                                .Style(PdfTheme.Observation).Justify();
                                        }
                                    });
                            });

                            x.Item().PaddingBottom(4).Text("5. Condições Socioeconômicas e Familiares").Style(PdfTheme.QuestionText);
                            x.Item().PaddingLeft(8).PaddingBottom(8).Text(socialText).Style(PdfTheme.AnswerText).Justify();
                            if (socialWorkerRecommendations.Count > 0 || !string.IsNullOrWhiteSpace(socialWorkerOtherRecommendations))
                            {
                                x.Item().PaddingBottom(2).Text("Recomendações do Serviço Social:").Style(PdfTheme.QuestionText);
                                x.Item().PaddingBottom(8).Column(inner =>
                                {
                                    inner.Item()
                                        .PaddingLeft(30)
                                        .Column(list =>
                                        {
                                            list.Spacing(4);

                                            foreach (var rec in socialWorkerRecommendations)
                                            {
                                                var label = rec.Recommendation.GetDisplayName();
                                                var isOther =
                                                    string.Equals(label, "Outro", StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(label, "Outra", StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(label, "Outros", StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(label, "Outras", StringComparison.OrdinalIgnoreCase);

                                                var text = isOther && !string.IsNullOrWhiteSpace(socialWorkerOtherRecommendations)
                                                    ? $"{label}: {socialWorkerOtherRecommendations}"
                                                    : label;

                                                list.Item().Row(r =>
                                                {
                                                    r.ConstantItem(12).Text("•").Style(PdfTheme.Bullet);
                                                    r.RelativeItem().Text(text).Style(PdfTheme.AnswerText).Justify();
                                                });
                                            }

                                            var hasOtherSelected = socialWorkerRecommendations.Any(r =>
                                                {
                                                    var label = r.Recommendation.GetDisplayName();
                                                    return
                                                        string.Equals(label, "Outro", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(label, "Outra", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(label, "Outros", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(label, "Outras", StringComparison.OrdinalIgnoreCase);
                                                });

                                            if (!hasOtherSelected && !string.IsNullOrWhiteSpace(socialWorkerOtherRecommendations))
                                            {
                                                list.Item().PaddingTop(6).Text($"Outras recomendações: {socialWorkerOtherRecommendations}")
                                                    .Style(PdfTheme.Observation).Justify();
                                            }
                                        });
                                });
                            }

                            x.Item().PaddingBottom(4).Text("6. Desenvolvimento Cognitivo").Style(PdfTheme.QuestionText);
                            x.Item().PaddingLeft(8).PaddingBottom(8).Text(desenvolvimentoCognitivo).Style(PdfTheme.AnswerText).Justify();

                            x.Item().PaddingBottom(4).Text("7. Desenvolvimento Emocional").Style(PdfTheme.QuestionText);
                            x.Item().PaddingLeft(8).PaddingBottom(8).Text(desenvolvimentoEmocional).Style(PdfTheme.AnswerText).Justify();

                            if (_report?.PsychologistReport?.AdaptiveBehaviorScaleDI != null)
                            {
                                x.Item().PaddingBottom(4).Text("8. Desenvolvimento Social").Style(PdfTheme.QuestionText);
                                x.Item().PaddingLeft(8).PaddingBottom(8).Text(desenvolvimentoSocial).Style(PdfTheme.AnswerText).Justify();
                            }

                            x.Item().PaddingBottom(4).Text("9. Observações Comportamentais").Style(PdfTheme.QuestionText);
                            x.Item().PaddingLeft(8).PaddingBottom(8).Text(_report?.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.ComportamentalObservations ?? "-").Style(PdfTheme.AnswerText).Justify();

                            x.Item().PaddingBottom(4).Text("10. Observações sobre o Caderno do Aluno").Style(PdfTheme.QuestionText);
                            x.Item().PaddingLeft(8).PaddingBottom(8).Text(_report?.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.NotebookObservations ?? "-").Style(PdfTheme.AnswerText).Justify();

                            x.Item().PaddingBottom(4).Text("11. Outras Observações Relevantes").Style(PdfTheme.QuestionText);
                            x.Item().PaddingLeft(8).PaddingBottom(8).Text(_report?.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.OtherObservations ?? "-").Style(PdfTheme.AnswerText).Justify();

                            x.Item().PaddingBottom(4).Text("12. Tipo de Deficiência").Style(PdfTheme.QuestionText);
                            x.Item().PaddingLeft(8).PaddingBottom(8).Text(disabilities).Style(PdfTheme.AnswerText).Justify();

                            x.Item().PaddingBottom(4).Text("13. Tipo de Transtorno").Style(PdfTheme.QuestionText);
                            x.Item().PaddingLeft(8).PaddingBottom(8).Text(disorders).Style(PdfTheme.AnswerText).Justify();

                            x.Item().PaddingBottom(4).Text("14. Recomendações da(o) Psicóloga(o)").Style(PdfTheme.QuestionText);
                            x.Item().PaddingBottom(8).Column(inner =>
                            {
                                inner.Item()
                                    .PaddingLeft(30)
                                    .Column(list =>
                                    {
                                        list.Spacing(4);

                                        foreach (var recommendation in psychologistRecommendations)
                                        {
                                            list.Item().Row(r =>
                                            {
                                                r.ConstantItem(12).Text("•").Style(PdfTheme.Bullet);
                                                r.RelativeItem().Text(recommendation).Style(PdfTheme.AnswerText).Justify();
                                            });
                                        }
                                    });
                            });

                            x.Item().PaddingBottom(4).Text("15. Recomendações do(a) Psicopedagogo(a)").Style(PdfTheme.QuestionText);
                            x.Item().PaddingBottom(8).Column(inner =>
                            {
                                inner.Item()
                                    .PaddingLeft(30)
                                    .Column(list =>
                                    {
                                        list.Spacing(4);

                                        foreach (var recommendation in _report?.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.Recommendations ?? Enumerable.Empty<PsychopedagogueQuestionnaire_Recommendation>())
                                        {
                                            list.Item().Row(r =>
                                            {
                                                r.ConstantItem(12).Text("•").Style(PdfTheme.Bullet);
                                                r.RelativeItem().Text(recommendation.Recommendation.GetDisplayName()).Style(PdfTheme.AnswerText).Justify();
                                            });
                                        }

                                        if (!string.IsNullOrWhiteSpace(_report?.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.OtherRecommendations))
                                        {
                                            list.Item().PaddingTop(6).Text($"Outras recomendações: {_report?.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.OtherRecommendations}")
                                                .Style(PdfTheme.Observation).Justify();
                                        }
                                    });
                            });

                            x.Item().PaddingBottom(4).Text("16. Conclusão").Style(PdfTheme.QuestionText);
                            x.Item().PaddingLeft(8).PaddingBottom(8).Text(conclusao).Style(PdfTheme.AnswerText).Justify();

                            // Bloco de Assinaturas (SignatureStamps) com status e horário de Brasília
                            x.Item().PaddingTop(12).PaddingBottom(4).Text("17. Assinaturas Digitais").Style(PdfTheme.QuestionText);
                            x.Item().PaddingLeft(8).PaddingBottom(8).Column(sigCol =>
                            {
                                sigCol.Spacing(6);

                                // 1. Assistente Social
                                var swName = _report?.SocialWorkerReport?.User?.FullName ?? _report?.User?.FullName ?? "Profissional Responsável";
                                var swSigned = _report?.SocialWorkerSign == true;
                                var swTime = _report?.SocialWorkerSignedAt != null
                                    ? _report.SocialWorkerSignedAt.Value.ToBrazilianTime().ToString("dd/MM/yyyy às HH:mm")
                                    : null;
                                var swStatus = swSigned ? $"Assinado digitalmente em {swTime}" : "Pendente de assinatura";
                                sigCol.Item().Row(r =>
                                {
                                    r.ConstantItem(12).Text("•").Style(PdfTheme.Bullet);
                                    r.RelativeItem().Text(t =>
                                    {
                                        t.Span("Assistente Social: ").SemiBold();
                                        t.Span($"{swName} — {swStatus}").Style(PdfTheme.AnswerText);
                                    });
                                });

                                // 2. Psicólogo(a)
                                var psiName = _report?.PsychologistReport?.PsychologistQuestionnaire?.User?.FullName 
                                    ?? _report?.PsychologistReport?.User?.FullName 
                                    ?? "Profissional Responsável";
                                var psiSigned = _report?.PsychologistSign == true;
                                var psiTime = _report?.PsychologistSignedAt != null
                                    ? _report.PsychologistSignedAt.Value.ToBrazilianTime().ToString("dd/MM/yyyy às HH:mm")
                                    : null;
                                var psiStatus = psiSigned ? $"Assinado digitalmente em {psiTime}" : "Pendente de assinatura";
                                sigCol.Item().Row(r =>
                                {
                                    r.ConstantItem(12).Text("•").Style(PdfTheme.Bullet);
                                    r.RelativeItem().Text(t =>
                                    {
                                        t.Span("Psicólogo(a): ").SemiBold();
                                        t.Span($"{psiName} — {psiStatus}").Style(PdfTheme.AnswerText);
                                    });
                                });

                                // 3. Psicopedagogo(a)
                                var pspName = _report?.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.User?.FullName 
                                    ?? _report?.PsychopedagogueReport?.User?.FullName 
                                    ?? "Profissional Responsável";
                                var pspSigned = _report?.PsychopedagogueSign == true;
                                var pspTime = _report?.PsychopedagogueSignedAt != null
                                    ? _report.PsychopedagogueSignedAt.Value.ToBrazilianTime().ToString("dd/MM/yyyy às HH:mm")
                                    : null;
                                var pspStatus = pspSigned ? $"Assinado digitalmente em {pspTime}" : "Pendente de assinatura";
                                sigCol.Item().Row(r =>
                                {
                                    r.ConstantItem(12).Text("•").Style(PdfTheme.Bullet);
                                    r.RelativeItem().Text(t =>
                                    {
                                        t.Span("Psicopedagogo(a): ").SemiBold();
                                        t.Span($"{pspName} — {pspStatus}").Style(PdfTheme.AnswerText);
                                    });
                                });
                            });

                            // Bloco de Validação do Documento com QR Code (Req 12.4, 12.5)
                            if (!string.IsNullOrWhiteSpace(_context.ValidationUrl))
                            {
                                x.Item().ShowEntire().PaddingTop(16).Element(PdfTheme.Card).Row(qrRow =>
                                {
                                    var qrSvg = QrCodeSvg.Render(_context.ValidationUrl, sizePx: 80);

                                    qrRow.AutoItem().PaddingRight(12).Width(80).Height(80).Svg(qrSvg);

                                    qrRow.RelativeItem().Column(infoCol =>
                                    {
                                        infoCol.Spacing(4);
                                        infoCol.Item().Text("Validação da Autenticidade do Documento").Style(PdfTheme.H2);
                                        infoCol.Item().Text("Aponte a câmera do celular para o código QR ou acesse o link abaixo para verificar a validade e o estado das assinaturas deste documento de forma pública e segura:")
                                            .Style(PdfTheme.AnswerText);
                                        infoCol.Item().Hyperlink(_context.ValidationUrl).Text(_context.ValidationUrl)
                                            .FontColor(PdfTheme.Colors.Accent).Underline().FontSize(9);
                                    });
                                });
                            }
                        });

                    page.Footer().Element(f => PdfBrandChrome.ComposeFooter(f, _context));
                });
            }
           
        }
    }
}
