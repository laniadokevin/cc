namespace CatchCornerStats.Core.Results
{
    public class SportComparisonResponseDto
    {
        public List<SportComparisonResult> Results { get; set; }
        public int TotalUniqueBookings { get; set; }
    }
} 