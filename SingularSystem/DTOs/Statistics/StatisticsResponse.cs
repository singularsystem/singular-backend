namespace SingularSystem.DTOs.Statistics
{
    public class StatisticsResponse
    {
        public int TotalStudents { get; set; }
        public double TotalStudentsComparedToLastMonth { get; set; }
        public int TotalReports { get; set; }
        public double TotalReportsComparedToLastMonth { get; set; }
        public int TotalCities { get; set; }
        public int TotalCitiesThisMonth { get; set; }
        public double ReportCompletionPercentage { get; set; }
        public double ReportCompletionPercentageComparedToLastMonth { get; set; }
        public int TotalStudentsLastMonth { get; set; }
        public int TotalReportsLast30Days { get; set; }
        public Dictionary<string, int> DistributionPerTypeOfDisability { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NumberOfReportsPerMonth { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NumberOfStudentsPerMonth { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NumberOfTotalRegistrationsPerCities { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NumberOfAEERegistrationsPerCities { get; set; } = new Dictionary<string, int>();

    }
}
