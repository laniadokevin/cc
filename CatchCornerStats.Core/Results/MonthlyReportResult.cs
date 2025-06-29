namespace CatchCornerStats.Core.Results
{
    public class MonthlyReportFacilityDto
    {
        public string FacilityName { get; set; }
        public Dictionary<string, int> MonthlyBookings { get; set; } = new();
    }

    public class MonthlyReportFlaggedDto
    {
        public string FacilityName { get; set; }
        public string MonthYear { get; set; }
        public int PreviousMonthBookings { get; set; }
        public int CurrentMonthBookings { get; set; }
        public double PercentageDrop { get; set; }
    }

    public class MonthlyReportResponseDto
    {
        public List<MonthlyReportFacilityDto> Facilities { get; set; } = new();
        public List<MonthlyReportFlaggedDto> FlaggedFacilities { get; set; } = new();
    }
}
