namespace CatchCornerStats.Core.Results
{
    public class BookingsByStartTimeResult
    {
        public int TotalBookings { get; set; }
        public Dictionary<string, int> Data { get; set; } = new();
    }
} 