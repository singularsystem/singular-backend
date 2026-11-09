using SingularSystem.DTOs.Reports;

namespace SingularSystem.DTOs.Statistics
{
    public class DashboardSummaryResponse
    {
        public int TotalStudents { get; set; }
        public int TotalReports { get; set; }
        public int TotalSchools { get; set; }
        public int TotalPAEEs { get; set; }
        public int TotalPEIsCompleted { get; set; }
        public int TotalPAEEsCompleted { get; set; }
        public int TotalCaseStudies { get; set; }
        public int TotalCaseStudiesCompleted { get; set; }
        public int TotalProcessesInProgress { get; set; }
        public int TotalBiopsychosocialReportsCompleted { get; set; }
        public int TotalBiopsychosocialReportsInProgress { get; set; }
        public int TotalBiopsychosocialReportsPendingSignature { get; set; }
        public int PsychologistFinished { get; set; }
        public int PsychologistInProgress { get; set; }
        public int PsychologistSigned { get; set; }
        public int PsychopedagogueFinished { get; set; }
        public int PsychopedagogueInProgress { get; set; }
        public int PsychopedagogueSigned { get; set; }
        public int SocialWorkerFinished { get; set; }
        public int SocialWorkerInProgress { get; set; }
        public int SocialWorkerSigned { get; set; }
        public int TotalPAEEsLast30Days { get; set; }
        public int TotalPEIsLast30Days { get; set; }
        public int TotalStudentsLastMonth { get; set; }
        public int TotalReportsLast30Days { get; set; }
        public Dictionary<string, int> DistributionPerGenre { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TypeOfRegistration { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TypeOfDisability { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> StudentsByAge { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NumberOfStudentsPerSchool { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NumberOfStudentsPerCity { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NumberOfConfiguredTotalStudentsPerCity { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NumberOfTotalRegistrationsPerMonth { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NumberOfAEERegistrationsPerMonth { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NumberOfReportsPerMonth { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NumberOfProfessionalsPerArea { get; set; } = new Dictionary<string, int>();
        public int TotalConfiguredStudentsAcrossCities { get; set; }
        public int SelectedCityRegisteredStudents { get; set; }
        public int SelectedCityConfiguredStudents { get; set; }
        public double SelectedCityCoveragePercentage { get; set; }
        public ICollection<ReportResponse> Reports { get; set; } = new List<ReportResponse>();
    }
}
