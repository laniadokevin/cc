namespace CatchCornerStats.Core.Results
{
    public class BookingsByDayDto
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public int BookingsCount { get; set; }
        public int TotalBookings { get; set; }
        public decimal Percentage { get; set; }
    }
} 