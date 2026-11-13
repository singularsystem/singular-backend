using SingularSystem.Domain.Tenancy;
using SingularSystem.Mappers;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Services
{
    public class StudentEvaluationDocumentService
    {
        private readonly AppDbContext _appDbContext;
        private readonly ITenantContext _tenantContext;

        public StudentEvaluationDocumentService(AppDbContext context, ITenantContext tenantContext)
        {
            _appDbContext = context;
            _tenantContext = tenantContext;
        }

        public async Task<Guid?> TryCreateUnifiedDocumentAsync(
            Guid screeningId,
            Guid questionnaireId,
            string currentUserId)
        {
            var screening = await _appDbContext.RegularTeacherScreenings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == screeningId);

            var questionnaire = await _appDbContext.RegularTeacherQuestionnaires
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == questionnaireId);

            if (screening == null || questionnaire == null)
                return null;

            if (screening.StudentID != questionnaire.StudentID)
                return null;

            var alreadyUsed = await _appDbContext.RegularTeacherEvaluationDocuments.AnyAsync(x =>
                x.RegularTeacherScreeningID == screeningId ||
                x.RegularTeacherQuestionnaireID == questionnaireId);

            if (alreadyUsed)
                return null;

            var evaluationDocument = RegularTeacherEvaluationDocumentMapper
                .ToEntity(currentUserId, screening, questionnaire);

            if (_tenantContext.IsPrivatePlan)
                evaluationDocument.TenantId = _tenantContext.TenantId;

            await _appDbContext.RegularTeacherEvaluationDocuments.AddAsync(evaluationDocument);
            await _appDbContext.SaveChangesAsync();

            return evaluationDocument.ID;
        }

        public async Task MarkAsOutdatedByScreeningAsync(Guid screeningId)
        {
            var document = await _appDbContext.RegularTeacherEvaluationDocuments.FirstOrDefaultAsync(d => d.RegularTeacherScreeningID == screeningId);

            ResetUnifiedDocument(document);
        }

        public async Task MarkAsOutdatedByQuestionnaireAsync(Guid questionnaireId)
        {
            var document = await _appDbContext.RegularTeacherEvaluationDocuments
                .FirstOrDefaultAsync(d => d.RegularTeacherQuestionnaireID == questionnaireId);

            ResetUnifiedDocument(document);
        }

        private static void ResetUnifiedDocument(RegularTeacherEvaluationDocument? document)
        {
            if (document == null)
                return;

            document.DocumentStatus = Status.InProgress;
            document.UnifiedAt = null;

            document.SignedAt = null;
            document.TeacherSign = false;
            document.SignUrl = null;
            document.SignedFile = null;

            document.UpdatedAt = DateTime.UtcNow;
        }
    }

}
