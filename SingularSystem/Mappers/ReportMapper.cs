using SingularSystem.DTOs.Reports;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;

namespace SingularSystem.Mappers
{
    public static class ReportMapper
    {
        private static UserSummaryResponse? FromCreators(string? userId, Dictionary<string, UserSummaryResponse>? creators) =>
            userId != null && creators != null && creators.TryGetValue(userId, out var u) ? u : null;

        public static async Task<ReportResponse> ToResponse(
            Report entity, IUserSummaryFactory userSummaryFactory
        )
        {
            var socialWorkerProfessional = entity.SocialWorkerReport?.User != null
            ? await userSummaryFactory.CreateAsync(entity.SocialWorkerReport.User)
            : null;

            var psychologistProfessional = (entity.PsychologistReport?.PsychologistQuestionnaire?.User ?? entity.PsychologistReport?.User) != null
            ? await userSummaryFactory.CreateAsync(entity.PsychologistReport?.PsychologistQuestionnaire?.User ?? entity.PsychologistReport?.User!)
            : null;

            var psychopedagogueProfessional = (entity.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.User ?? entity.PsychopedagogueReport?.User) != null
            ? await userSummaryFactory.CreateAsync(entity.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.User ?? entity.PsychopedagogueReport?.User!)
            : null;

            var status = entity.ReportStatus;
            if (status != Models.Enums.Status.Complete && 
                (entity.IsReadyForUnification() || 
                 entity.SignatureBlocked || 
                 entity.DocumentStatus == Models.Enums.Status.Complete || 
                 (entity.SocialWorkerSign && entity.PsychologistSign && entity.PsychopedagogueSign)))
            {
                status = Models.Enums.Status.Complete;
            }

            return new ReportResponse()
            {
                ID = entity.ID,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                PsychologistReport = entity.PsychologistReport != null ? PsychologistReportMapper.ToResponse(entity.PsychologistReport, psychologistProfessional, entity.Student) : null,
                PsychopedagogueReport = entity.PsychopedagogueReport != null ? PsychopedagogueReportMapper.ToResponse(entity.PsychopedagogueReport, psychopedagogueProfessional, entity.Student) : null,
                SocialWorkerReport = entity.SocialWorkerReport != null ? SocialWorkerReportMapper.ToResponse(entity.SocialWorkerReport, socialWorkerProfessional) : null,
                Status = status.GetDisplayName(),
                TypeOfDisability = entity.GetCombinedDisabilitiesAndDisorders(),
                ReportConfirmed = entity.ReportConfirmed,
                DocumentToken = entity.DocumentToken,
                SignedFile = entity.SignedFile,
                DocumentStatus = entity.DocumentStatus.GetDisplayName() ?? "",
                SignatureBlocked = entity.SignatureBlocked,
                SocialWorkerSign = entity.SocialWorkerSign,
                PsychologistSign = entity.PsychologistSign,
                PsychopedagogueSign = entity.PsychopedagogueSign,
                SocialWorkerSignUrl = entity.SocialWorkerSignUrl ?? "",
                PsychopedagogueSignUrl = entity.PsychopedagogueSignUrl ?? "",
                PsychologistSignUrl = entity.PsychologistSignUrl ?? "",
                SocialWorkerSignedAt = entity.SocialWorkerSignedAt,
                PsychopedagogueSignedAt = entity.PsychopedagogueSignedAt,
                PsychologistSignedAt = entity.PsychologistSignedAt,
                CreatedAt = entity.Student?.CreatedAt ?? entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                UnifiedAt = entity.UnifiedAt,
            };
        }

        public static ReportSummaryResponse ToSummaryResponse(
            Report entity,
            Dictionary<string, UserSummaryResponse>? creators = null
        )
        {
            var status = entity.ReportStatus;
            if (status != Models.Enums.Status.Complete && 
                (entity.IsReadyForUnification() || 
                 entity.SignatureBlocked || 
                 entity.DocumentStatus == Models.Enums.Status.Complete || 
                 (entity.SocialWorkerSign && entity.PsychologistSign && entity.PsychopedagogueSign)))
            {
                status = Models.Enums.Status.Complete;
            }

            var psychologistUserId = entity.PsychologistReport?.PsychologistQuestionnaire?.UserID ?? entity.PsychologistReport?.UserID;
            var psychopedagogueUserId = entity.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.UserID ?? entity.PsychopedagogueReport?.UserID;

            return new ReportSummaryResponse()
            {
                ID = entity.ID,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                PsychologistReport = entity.PsychologistReport != null ? PsychologistReportMapper.ToSummaryResponse(entity.PsychologistReport, FromCreators(psychologistUserId, creators), entity.Student) : null,
                PsychopedagogueReport = entity.PsychopedagogueReport != null ? PsychopedagogueReportMapper.ToSummaryResponse(entity.PsychopedagogueReport, FromCreators(psychopedagogueUserId, creators), entity.Student) : null,
                SocialWorkerReport = entity.SocialWorkerReport != null ? SocialWorkerReportMapper.ToSummaryResponse(entity.SocialWorkerReport, FromCreators(entity.SocialWorkerReport.UserID, creators)) : null,
                DocumentToken = entity.DocumentToken,
                Status = status.GetDisplayName(),
                TypeOfDisability = entity.GetCombinedDisabilitiesAndDisorders(),
                SignedFile = entity.SignedFile,
                DocumentStatus = entity.DocumentStatus.GetDisplayName() ?? "",
                SignatureBlocked = entity.SignatureBlocked,
                SocialWorkerSign = entity.SocialWorkerSign,
                PsychologistSign = entity.PsychologistSign,
                PsychopedagogueSign = entity.PsychopedagogueSign,
                SocialWorkerSignUrl = entity.SocialWorkerSignUrl ?? "",
                PsychopedagogueSignUrl = entity.PsychopedagogueSignUrl ?? "",
                PsychologistSignUrl = entity.PsychologistSignUrl ?? "",
                SocialWorkerSignedAt = entity.SocialWorkerSignedAt,
                PsychopedagogueSignedAt = entity.PsychopedagogueSignedAt,
                PsychologistSignedAt = entity.PsychologistSignedAt,
                CreatedAt = entity.Student?.CreatedAt ?? entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                UnifiedAt = entity.UnifiedAt,
            };
        }

        public static ReportSummaryResponse FromStudent(
            Student student
        )
        {
            return new ReportSummaryResponse()
            {
                ID = Guid.Empty,
                Student = StudentMapper.ToSummaryResponse(student),
                PsychologistReport = null,
                PsychopedagogueReport = null,
                SocialWorkerReport = null,
                Status = "Não Iniciado",
                TypeOfDisability = "",
                DocumentStatus = "Pendente",
                CreatedAt = student.CreatedAt
            };
        }
    }
}
