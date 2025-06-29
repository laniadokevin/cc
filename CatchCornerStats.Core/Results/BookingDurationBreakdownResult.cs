namespace CatchCornerStats.Core.Results
{
    public class BookingDurationBreakdownResult
    {
        public int TotalBookings { get; set; }
        public Dictionary<string, int> Data { get; set; } = new();
        public double AverageDuration { get; set; }
    }
} 