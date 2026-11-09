namespace SingularSystem.DTOs.Integrations
{
    public class ActivitiesSummaryIntegrationResponse
    {
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }

        public int TeacherScreening { get; set; }
        public int CaseStudiesInProgress { get; set; }
        public int CaseStudiesCompleted { get; set; }

        public int Paee { get; set; }
        public int Pei { get; set; }

        public int StudentsWithDisabilitiesAee { get; set; }
        public int DisordersWithDisability { get; set; }
        public int DisordersWithoutDisability { get; set; }

        public int ReportsInProgress { get; set; }
        public int ReportsCompleted { get; set; }

        public int TotalStudentsWithDisorders { get; set; }
        public int TotalStudentsWithDisabilities { get; set; }

        public List<ItemCountDto> DisabilityTypes { get; set; } = new();
        public List<ItemCountDto> DisorderTypes { get; set; } = new();
    }

    public class ItemCountDto
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
