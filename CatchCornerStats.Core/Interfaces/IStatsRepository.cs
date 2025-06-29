using CatchCornerStats.Core.Results;

namespace CatchCornerStats.Core.Interfaces
{
    /// <summary>
    /// Repository interface for retrieving booking-related statistics and reports.
    /// </summary>
    public interface IStatsRepository
    {
        /// <summary>
        /// Calculates the average lead time (in days) between order placement and booking date.
        /// </summary>
        /// <param name="sport">Sport to filter by (optional).</param>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="rinkSize">Rink size to filter by (optional).</param>
        /// <param name="facility">Facility to filter by (optional).</param>
        /// <returns>Average lead time in days.</returns>
        Task<double> GetAverageLeadTimeAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities);

        /// <summary>
        /// Returns the percentage breakdown of bookings by lead time (in days).
        /// </summary>
        /// <param name="sport">Sport to filter by (optional).</param>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="rinkSize">Rink size to filter by (optional).</param>
        /// <param name="facility">Facility to filter by (optional).</param>
        /// <param name="createdDateFrom">Start date to filter bookings (optional).</param>
        /// <param name="createdDateTo">End date to filter bookings (optional).</param>
        /// <param name="happeningDateFrom">Start date to filter bookings (optional).</param>
        /// <param name="happeningDateTo">End date to filter bookings (optional).</param>
        /// <returns>
        /// Dictionary where key = number of days before booking (as string, e.g. "0", "1", ..., "+30"), 
        /// value = percentage of bookings.
        /// </returns>
        Task<Dictionary<string, double>> GetLeadTimeBreakdownAsync(List<string>? sport, List<string>? city, List<string>? rinkSize, List<string>? facility, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo);

        /// <summary>
        /// Returns the percentage distribution of bookings by day of the week.
        /// </summary>
        /// <param name="sport">Sport to filter by (optional).</param>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="rinkSize">Rink size to filter by (optional).</param>
        /// <param name="facility">Facility to filter by (optional).</param>
        /// <param name="month">Month to filter by (optional).</param>
        /// <param name="createdDateFrom">Start date to filter bookings (optional).</param>
        /// <param name="createdDateTo">End date to filter bookings (optional).</param>
        /// <param name="happeningDateFrom">Start date to filter bookings (optional).</param>
        /// <param name="happeningDateTo">End date to filter bookings (optional).</param>
        /// <returns>
        /// Dictionary where key = day of the week (e.g., "Monday"), 
        /// value = percentage of bookings.
        /// </returns>
        Task<Dictionary<string, double>> GetBookingsByDayAsync(string? sport, string? city, string? rinkSize, string? facility, int? month, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo);

        /// <summary>
        /// Returns the percentage distribution of bookings by start time.
        /// </summary>
        /// <param name="sports">Sports to filter by (optional).</param>
        /// <param name="cities">Cities to filter by (optional).</param>
        /// <param name="rinkSizes">Rink sizes to filter by (optional).</param>
        /// <param name="facilities">Facilities to filter by (optional).</param>
        /// <param name="month">Month to filter by (optional).</param>
        /// <param name="dayOfWeek">Day of the week to filter by (optional).</param>
        /// <param name="createdDateFrom">Start date to filter bookings (optional).</param>
        /// <param name="createdDateTo">End date to filter bookings (optional).</param>
        /// <param name="happeningDateFrom">Start date to filter bookings (optional).</param>
        /// <param name="happeningDateTo">End date to filter bookings (optional).</param>
        /// <returns>
        /// BookingsByStartTimeResult with total bookings and data dictionary where key = start time (e.g., "9:00 AM"), 
        /// value = number of bookings.
        /// </returns>
        Task<BookingsByStartTimeResult> GetBookingsByStartTimeAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities, int? month, int? dayOfWeek, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo);

        /// <summary>
        /// Returns the percentage distribution of booking durations.
        /// </summary>
        /// <param name="sport">Sport to filter by (optional).</param>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="rinkSize">Rink size to filter by (optional).</param>
        /// <param name="facility">Facility to filter by (optional).</param>
        /// <param name="createdDateFrom">Start date to filter bookings (optional).</param>
        /// <param name="createdDateTo">End date to filter bookings (optional).</param>
        /// <param name="happeningDateFrom">Start date to filter bookings (optional).</param>
        /// <param name="happeningDateTo">End date to filter bookings (optional).</param>
        /// <returns>
        /// Dictionary where key = duration (e.g., "1 hr"), 
        /// value = percentage of bookings.
        /// </returns>
        Task<Dictionary<string, double>> GetBookingDurationBreakdownAsync(string? sport, string? city, string? rinkSize, string? facility, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo);

        /// <summary>
        /// Returns the number of bookings per facility, month by month.
        /// Also flags facilities with a 50%+ drop in bookings from the previous month.
        /// </summary>
        /// <param name="sport">Sport to filter by (optional).</param>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="rinkSize">Rink size to filter by (optional).</param>
        /// <param name="facility">Facility to filter by (optional).</param>
        /// <returns>
        /// List of MonthlyReportResult with the number of bookings and flag status.
        /// </returns>
        Task<List<MonthlyReportResult>> GetMonthlyReportAsync(string? sport, string? city, string? rinkSize, string? facility);

        /// <summary>
        /// Compares sport bookings to actual results on a per-city basis.
        /// Flags sports that:
        /// - Are not in the top 6 but have more bookings than a top 6 sport.
        /// - Are not in the top 8 but have more bookings than a top 8 sport.
        /// - Have 60+ bookings (or >= 5% of the most popular sport) in a month.
        /// </summary>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="month">Month to filter by (optional).</param>
        /// <returns>
        /// List of SportComparisonResult containing sport ranking and flag status.
        /// </returns>
        Task<List<SportComparisonResult>> GetSportComparisonReportAsync(string? city, int? month);
        Task<List<string>> GetSportsAsync();
        Task<List<string>> GetCitiesAsync();
        Task<List<string>> GetRinkSizesAsync();
        Task<List<string>> GetFacilitiesAsync();
        Task<List<StatsRawDto>> GetAllStatsRawAsync();
        Task<(double averageLeadTime, int totalBookings)> GetAverageLeadTimeWithCountAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo);
        Task<List<BookingsByDayDto>> GetBookingsByDayReportAsync(
            List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities, int? month, int? year,
            DateTime? createdDateFrom = null, DateTime? createdDateTo = null, DateTime? happeningDateFrom = null, DateTime? happeningDateTo = null);
    }
}
