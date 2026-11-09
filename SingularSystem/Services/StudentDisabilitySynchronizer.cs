using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Services
{
    public static class StudentDisabilitySynchronizer
    {
        public static async Task SynchronizeCARSTeaAsync(AppDbContext dbContext, Guid studentId)
        {
            // 1. Obter o estudante
            var student = await dbContext.Students
                .FirstOrDefaultAsync(s => s.ID == studentId);
            if (student == null) return;

            // 2. Localizar o último Relatório Psicopedagógico do aluno
            var pReport = await dbContext.PsychopedagogueReports
                .Include(r => r.PsychopedagogueQuestionnaire)
                .ThenInclude(q => q.TypeOfDisability)
                .Include(r => r.CARSQuestionnaire)
                .Where(r => r.StudentID == studentId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (pReport == null) return;

            // 3. Garantir a existência do Questionário Psicopedagógico (ficha principal)
            var pQ = pReport.PsychopedagogueQuestionnaire;
            if (pQ == null)
            {
                pQ = new PsychopedagogueQuestionnaire
                {
                    StudentID = studentId,
                    UserID = pReport.UserID,
                    PsychopedagogueReportID = pReport.ID,
                    ComportamentalObservations = string.Empty,
                    NotebookObservations = string.Empty,
                    JustificationForClassification = "Classificação de deficiência atualizada automaticamente baseada nas pontuações dos testes realizados."
                };
                await dbContext.PsychopedagogueQuestionnaires.AddAsync(pQ);
                await dbContext.SaveChangesAsync();
                
                // Recarregar com a coleção de deficiências incluída
                pQ = await dbContext.PsychopedagogueQuestionnaires
                    .Include(q => q.TypeOfDisability)
                    .FirstOrDefaultAsync(q => q.ID == pQ.ID);
            }

            if (pQ == null) return;

            // 4. Verificar se a pontuação do CARS aponta autismo (não sendo 'Até 30')
            var cars = pReport.CARSQuestionnaire;
            bool shouldHaveTEA = cars != null && (cars.Result == CARSResult.SevereAutism || cars.Result == CARSResult.MildToModerateAutism);

            // 5. Atualizar o TypeOfDisability para incluir ou remover o TEA
            var existingTea = pQ.TypeOfDisability?.FirstOrDefault(d => d.Disability == TypeOfDisability.TEA);

            if (shouldHaveTEA)
            {
                if (existingTea == null)
                {
                    var newDisability = new PsychopedagogueQuestionnaire_TypeOfDisability
                    {
                        ID = Guid.NewGuid(),
                        PsychopedagogueQuestionnaireID = pQ.ID,
                        Disability = TypeOfDisability.TEA
                    };
                    await dbContext.PsychopedagogueQuestionnaire_TypeOfDisabilities.AddAsync(newDisability);
                    await dbContext.SaveChangesAsync();
                }
            }
            else
            {
                if (existingTea != null)
                {
                    dbContext.PsychopedagogueQuestionnaire_TypeOfDisabilities.Remove(existingTea);
                    await dbContext.SaveChangesAsync();
                }
            }

            // 6. Atualizar o status de matrícula do aluno (AEE ou Geral)
            var activeDisabilities = await dbContext.PsychopedagogueQuestionnaire_TypeOfDisabilities
                .Where(td => td.PsychopedagogueQuestionnaireID == pQ.ID && td.Disability != TypeOfDisability.None)
                .CountAsync();

            bool hasDisability = activeDisabilities > 0;
            string targetType = hasDisability ? "AEE" : "Geral";

            if (student.RegistrationType != targetType)
            {
                student.RegistrationType = targetType;
                student.UpdatedAt = DateTime.UtcNow;
                dbContext.Students.Update(student);
                await dbContext.SaveChangesAsync();
            }

            // 7. Atualizar o relatório consolidado (Report), caso ele já esteja unificado
            if (pReport.ReportID.HasValue)
            {
                var report = await dbContext.Reports
                    .Include(r => r.Student)
                    .Include(r => r.SocialWorkerReport)
                    .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.AdaptiveBehaviorScaleDI)
                    .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.SnapIVQuestionnaire)
                    .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.PsychologistQuestionnaire)
                    .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.PsychopedagogueQuestionnaire).ThenInclude(pq => pq.TypeOfDisability)
                    .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.PsychopedagogueQuestionnaire).ThenInclude(pq => pq.TypeOfDisorder)
                    .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.CARSQuestionnaire)
                    .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.MCHATQuestionnaire)
                    .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.CognitiveAssessment)
                    .FirstOrDefaultAsync(r => r.ID == pReport.ReportID.Value);

                if (report != null && report.IsReadyForUnification())
                {
                    report.UnifyReport();
                    dbContext.Reports.Update(report);
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
