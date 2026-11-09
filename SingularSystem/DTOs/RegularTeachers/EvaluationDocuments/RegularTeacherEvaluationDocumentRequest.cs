namespace SingularSystem.DTOs.RegularTeachers.EvaluationDocuments
{
    public class RegularTeacherEvaluationDocumentRequest
    {
        public Guid StudentID { get; set; }
        public string UserID { get; set; }
        public Guid RegularTeacherScreeningID { get; set; }
        public Guid NeuropsychopedagogicalQuestionnaireID { get; set; }
    }
}
