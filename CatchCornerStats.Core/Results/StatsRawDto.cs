namespace CatchCornerStats.Core.Results
{
    public class StatsRawDto
    {
        public int BookingNumber { get; set; }
        public DateTime CreatedDateUtc { get; set; }
        public DateTime HappeningDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Sport { get; set; }
        public string City { get; set; }
        public string RinkSize { get; set; }
        public string Facility { get; set; }
    }
}
