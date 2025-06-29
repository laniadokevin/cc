namespace CatchCornerStats.Core.Results
{
    public class MonthlyReportResult
    {
        public string FacilityName { get; set; }
        public string MonthYear { get; set; }
        public int TotalBookings { get; set; }
        public bool IsFlagged { get; set; }
        public int? PreviousMonthBookings { get; set; }
        public double? PercentageDrop { get; set; }
    }
}
