using SingularSystem.DTOs.RegularTeachers.EvaluationDocuments;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class RegularTeacherEvaluationDocumentMapper
    {
        public static RegularTeacherEvaluationDocument ToEntity(
            string userId,
            RegularTeacherScreening RegularTeacherScreening,
            RegularTeacherQuestionnaire NeuropsychopedagogicalQuestionnaire
        )
        {
            return new RegularTeacherEvaluationDocument
            {
                StudentID = RegularTeacherScreening.StudentID,
                UserID = userId,
                RegularTeacherScreeningID = RegularTeacherScreening.ID,
                RegularTeacherQuestionnaireID = NeuropsychopedagogicalQuestionnaire.ID,
                EvaluationStatus = Status.InProgress,
            };
        }

        public static RegularTeacherEvaluationDocumentResponse ToResponse(
            RegularTeacherEvaluationDocument entity, UserSummaryResponse? createdBy, string? status = null, Student? student = null
        )
        {
            return new RegularTeacherEvaluationDocumentResponse
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                Screening = entity.RegularTeacherScreening != null ? RegularTeacherScreeningMapper.ToResponse(entity.RegularTeacherScreening, null) : null,
                Questionnaire = entity.RegularTeacherQuestionnaire != null ? RegularTeacherQuestionnaireMapper.ToResponse(entity.RegularTeacherQuestionnaire, null) : null,
                DocumentToken = entity.DocumentToken,
                SignedFile = entity.SignedFile,
                DocumentStatus = entity.DocumentStatus.GetDisplayName(),
                TeacherSign = entity.TeacherSign,
                SignUrl = entity.SignUrl ?? "",
                CreatedAt = student?.CreatedAt ?? entity.Student?.CreatedAt ?? entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                SignedAt = entity.SignedAt,
                UnifiedAt = entity.UnifiedAt,
                Status = status ?? (entity.TeacherSign ? "Completo" : "Incompleto")
            };
        }

        public static RegularTeacherEvaluationDocumentResponse FromStudent(
            Student student, string status
        )
        {
            return new RegularTeacherEvaluationDocumentResponse
            {
                ID = Guid.Empty,
                Student = StudentMapper.ToSummaryResponse(student),
                Status = status,
                DocumentStatus = "Pendente",
                SignUrl = "",
                CreatedAt = student.CreatedAt,
            };
        }
    }
}
