namespace CatchCornerStats.Core.Results
{
    public class SportComparisonResult
    {
        public string Sport { get; set; }
        public string City { get; set; }
        public int TotalBookings { get; set; }
        public int Ranking { get; set; }
        public bool IsFlaggedTop6 { get; set; }
        public bool IsFlaggedTop8 { get; set; }
        public bool IsFlaggedHighBookings { get; set; }
    }
}
