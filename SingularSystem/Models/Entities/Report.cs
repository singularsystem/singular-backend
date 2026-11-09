using SingularSystem.Extensions;
using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class Report : BaseDocument
    {
        public string? BodySocialWorker { get; set; }
        public string? BodyPsychologist { get; set; }
        public string? BodyPsychopedagogue { get; set; }
        public string? TypeDisability { get; set; }
        public bool ReportConfirmed { get; set; } = false;
        public Status ReportStatus { get; set; } = Status.None;

        public Guid? SocialWorkerReportID { get; set; }
        public Guid? PsychologistReportID { get; set; }
        public Guid? PsychopedagogueReportID { get; set; }

        public SocialWorkerReport? SocialWorkerReport { get; set; }
        public PsychologistReport? PsychologistReport { get; set; }
        public PsychopedagogueReport? PsychopedagogueReport { get; set; }

        //ZapSign
        public string? DocumentToken { get; set; }
        public string? SignedFile { get; set; }
        public Status DocumentStatus { get; set; } = Status.None;
        public SignatureProvider SignatureProvider { get; set; } = SignatureProvider.NeckSign;
        public bool SocialWorkerSign { get; set; } = false;
        public bool PsychologistSign { get; set; } = false;
        public bool PsychopedagogueSign { get; set; } = false;
        public string? SocialWorkerSignUrl { get; set; }
        public string? PsychologistSignUrl { get; set; }
        public string? PsychopedagogueSignUrl { get; set; }
        public DateTime? SocialWorkerSignedAt { get; set; }
        public DateTime? PsychologistSignedAt { get; set; }
        public DateTime? PsychopedagogueSignedAt { get; set; }

        /// <summary>
        /// Indica que este relatório foi marcado administrativamente como "assinado"
        /// sem ter passado pelo fluxo real do ZapSign. Aplica-se a relatórios completos
        /// criados antes de 18/08/2026 que nunca foram assinados digitalmente.
        /// Visível apenas para administradores como "Assinatura Bloqueada".
        /// </summary>
        public bool SignatureBlocked { get; set; } = false;


        public string GetCombinedDisabilitiesAndDisorders()
        {
            var items = new List<string>();

            if (PsychopedagogueReport?.PsychopedagogueQuestionnaire != null)
            {
                if (PsychopedagogueReport.PsychopedagogueQuestionnaire.TypeOfDisability != null)
                {
                    var disabilities = PsychopedagogueReport.PsychopedagogueQuestionnaire.TypeOfDisability
                        .Where(d => d.Disability != TypeOfDisability.None)
                        .Select(d => d.Disability.GetDisplayName());
                    items.AddRange(disabilities);
                }

                if (PsychopedagogueReport.PsychopedagogueQuestionnaire.TypeOfDisorder != null)
                {
                    var disorders = PsychopedagogueReport.PsychopedagogueQuestionnaire.TypeOfDisorder
                        .Where(d => d.Disorder != TypeOfDisorder.None)
                        .Select(d => d.Disorder.GetDisplayName());
                    items.AddRange(disorders);
                }
            }

            if (PsychologistReport?.SnapIVQuestionnaire != null &&
                PsychologistReport.SnapIVQuestionnaire.Result != SnapIVResult.None)
            {
                var snapResult = PsychologistReport.SnapIVQuestionnaire.Result.GetDisplayName();
                items.Add(snapResult);
            }

            var uniqueItems = items
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals("Nenhum(a)", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList();

            if (uniqueItems.Count > 0)
            {
                return string.Join(", ", uniqueItems);
            }

            return !string.IsNullOrWhiteSpace(TypeDisability) ? TypeDisability : "";
        }

        public void UnifyReport()
        {
            if (SocialWorkerReport != null &&
                PsychologistReport != null &&
                PsychopedagogueReport != null)
            {
                TypeDisability = GetCombinedDisabilitiesAndDisorders();
                UnifiedAt = DateTime.UtcNow;
                ReportStatus = Status.Complete;
                ReportConfirmed = true;

                var hasDisability = PsychopedagogueReport.PsychopedagogueQuestionnaire?.TypeOfDisability?.Any(d => d.Disability != TypeOfDisability.None) == true;

                if (Student != null)
                {
                    Student.RegistrationType = hasDisability ? "AEE" : "Geral";
                }
            }
        }

        public bool IsReadyForUnification()
        {
            bool hasPsychologistAdaptive = PsychologistReport?.AdaptiveBehaviorScaleDI != null;
            bool hasPsychopedagogueCognitive = PsychopedagogueReport?.CognitiveAssessment != null;

            if (Student != null)
            {
                int age = DateTime.UtcNow.Year - Student.DateOfBirth.Year;
                if (Student.DateOfBirth.Date > DateTime.UtcNow.Date.AddYears(-age)) age--;

                if (age < 6)
                {
                    hasPsychologistAdaptive = true; // Ignore this requirement
                    hasPsychopedagogueCognitive = true; // Ignore this requirement
                }
            }

            return SocialWorkerReport != null &&
                    PsychologistReport?.PsychologistQuestionnaire != null &&
                    hasPsychologistAdaptive &&
                    PsychologistReport?.SnapIVQuestionnaire != null &&
                    (PsychopedagogueReport?.CARSQuestionnaire != null || PsychopedagogueReport?.MCHATQuestionnaire != null) &&
                    PsychopedagogueReport?.PsychopedagogueQuestionnaire != null &&
                    hasPsychopedagogueCognitive;
        }
    }
}
