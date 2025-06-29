namespace CatchCornerStats.Core.Results
{
    public class MonthlyReportGlobalDto
    {
        public string? MonthYear { get; set; }
        public int TotalBookings { get; set; }
        public int? PreviousMonthBookings { get; set; }
        public decimal? PercentageChange { get; set; }
    }
} 