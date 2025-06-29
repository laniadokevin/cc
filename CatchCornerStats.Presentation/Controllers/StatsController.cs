using CatchCornerStats.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CatchCornerStats.Presentation.Controllers
{
    /// <summary>
    /// Controller that exposes endpoints for retrieving booking-related statistics and reports.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly IStatsRepository _statsRepository;
        private readonly ILogger<StatsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatsController"/> class.
        /// </summary>
        /// <param name="statsRepository">The repository for retrieving statistics data.</param>
        /// <param name="logger">The logger for performance tracking.</param>
        public StatsController(IStatsRepository statsRepository, ILogger<StatsController> logger)
        {
            _statsRepository = statsRepository;
            _logger = logger;
        }

        [HttpGet("GetAllStatsRaw")]
        public async Task<IActionResult> GetAllStatsRaw()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _statsRepository.GetAllStatsRawAsync();
                stopwatch.Stop();
                _logger.LogInformation($"GetAllStatsRaw completed in {stopwatch.ElapsedMilliseconds}ms");
                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetAllStatsRaw failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        /// <summary>
        /// Get the average lead time (in days) between order placement and booking date.
        /// </summary>
        /// <param name="sport">Sport to filter by (optional).</param>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="rinkSize">Rink size to filter by (optional).</param>
        /// <param name="facility">Facility to filter by (optional).</param>
        /// <returns>The average lead time in days.</returns>
        [HttpGet("GetAverageLeadTime")]
        public async Task<IActionResult> GetAverageLeadTime(
            [FromQuery] List<string>? sports,
            [FromQuery] List<string>? cities,
            [FromQuery] List<string>? rinkSizes,
            [FromQuery] List<string>? facilities,
            [FromQuery] DateTime? createdDateFrom,
            [FromQuery] DateTime? createdDateTo,
            [FromQuery] DateTime? happeningDateFrom,
            [FromQuery] DateTime? happeningDateTo)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var (averageLeadTime, totalBookings) = await _statsRepository.GetAverageLeadTimeWithCountAsync(sports, cities, rinkSizes, facilities, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);
                stopwatch.Stop();
                _logger.LogInformation($"GetAverageLeadTime completed in {stopwatch.ElapsedMilliseconds}ms - Result: {averageLeadTime:F2} days, {totalBookings} bookings");
                return Ok(new { averageLeadTime, totalBookings });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetAverageLeadTime failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        /// <summary>
        /// Get the percentage breakdown of lead time (in days).
        /// </summary>
        /// <param name="sport">Sport to filter by (optional).</param>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="rinkSize">Rink size to filter by (optional).</param>
        /// <param name="facility">Facility to filter by (optional).</param>
        /// <returns>A dictionary where key = lead time in days, value = percentage of bookings.</returns>
        [HttpGet("GetLeadTimeBreakdown")]
        public async Task<IActionResult> GetLeadTimeBreakdown(
            [FromQuery] List<string>? sports,
            [FromQuery] List<string>? cities,
            [FromQuery] List<string>? rinkSizes,
            [FromQuery] List<string>? facilities,
            [FromQuery] DateTime? createdDateFrom,
            [FromQuery] DateTime? createdDateTo,
            [FromQuery] DateTime? happeningDateFrom,
            [FromQuery] DateTime? happeningDateTo)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _statsRepository.GetLeadTimeBreakdownAsync(sports, cities, rinkSizes, facilities, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);
                stopwatch.Stop();
                _logger.LogInformation($"GetLeadTimeBreakdown completed in {stopwatch.ElapsedMilliseconds}ms - {result.Count} breakdown items");
                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetLeadTimeBreakdown failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        /// <summary>
        /// Get the percentage distribution of bookings by day of the week.
        /// </summary>
        /// <param name="sport">Sport to filter by (optional).</param>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="rinkSize">Rink size to filter by (optional).</param>
        /// <param name="facility">Facility to filter by (optional).</param>
        /// <param name="month">Month to filter by (optional).</param>
        /// <returns>A dictionary where key = day of the week, value = percentage of bookings.</returns>
        [HttpGet("GetBookingsByDay")]
        public async Task<IActionResult> GetBookingsByDay(
            [FromQuery] string? sport,
            [FromQuery] string? city,
            [FromQuery] string? rinkSize,
            [FromQuery] string? facility,
            [FromQuery] int? month,
            [FromQuery] DateTime? createdDateFrom,
            [FromQuery] DateTime? createdDateTo,
            [FromQuery] DateTime? happeningDateFrom,
            [FromQuery] DateTime? happeningDateTo)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _statsRepository.GetBookingsByDayAsync(sport, city, rinkSize, facility, month, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);
                stopwatch.Stop();
                _logger.LogInformation($"GetBookingsByDay completed in {stopwatch.ElapsedMilliseconds}ms - {result.Count} days");
                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetBookingsByDay failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        /// <summary>
        /// Get the percentage distribution of bookings by start time.
        /// </summary>
        /// <param name="sport">Sport to filter by (optional).</param>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="rinkSize">Rink size to filter by (optional).</param>
        /// <param name="facility">Facility to filter by (optional).</param>
        /// <param name="month">Month to filter by (optional).</param>
        /// <param name="dayOfWeek">Day of the week to filter by (optional).</param>
        /// <returns>Un objeto con totalBookings y data (diccionario de hora a cantidad).</returns>
        [HttpGet("GetBookingsByStartTime")]
        public async Task<IActionResult> GetBookingsByStartTime(
            [FromQuery] string? sport,
            [FromQuery] string? city,
            [FromQuery] string? rinkSize,
            [FromQuery] string? facility,
            [FromQuery] int? month,
            [FromQuery] int? dayOfWeek,
            [FromQuery] DateTime? createdDateFrom,
            [FromQuery] DateTime? createdDateTo,
            [FromQuery] DateTime? happeningDateFrom,
            [FromQuery] DateTime? happeningDateTo)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _statsRepository.GetBookingsByStartTimeAsync(sport, city, rinkSize, facility, month, dayOfWeek, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);
                stopwatch.Stop();
                _logger.LogInformation($"GetBookingsByStartTime completed in {stopwatch.ElapsedMilliseconds}ms - {result.TotalBookings} total bookings, {result.Data.Count} time slots");
                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetBookingsByStartTime failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        /// <summary>
        /// Get the percentage distribution of bookings by duration.
        /// </summary>
        /// <param name="sport">Sport to filter by (optional).</param>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="rinkSize">Rink size to filter by (optional).</param>
        /// <param name="facility">Facility to filter by (optional).</param>
        /// <returns>A dictionary where key = duration (in hours), value = percentage of bookings.</returns>
        [HttpGet("GetBookingDurationBreakdown")]
        public async Task<IActionResult> GetBookingDurationBreakdown(
            [FromQuery] string? sport,
            [FromQuery] string? city,
            [FromQuery] string? rinkSize,
            [FromQuery] string? facility,
            [FromQuery] DateTime? createdDateFrom,
            [FromQuery] DateTime? createdDateTo,
            [FromQuery] DateTime? happeningDateFrom,
            [FromQuery] DateTime? happeningDateTo)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _statsRepository.GetBookingDurationBreakdownAsync(sport, city, rinkSize, facility, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);
                stopwatch.Stop();
                _logger.LogInformation($"GetBookingDurationBreakdown completed in {stopwatch.ElapsedMilliseconds}ms - {result.Count} duration categories");
                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetBookingDurationBreakdown failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        /// <summary>
        /// Get the monthly booking report, with flags for 50%+ drop in bookings.
        /// </summary>
        /// <param name="sport">Sport to filter by (optional).</param>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="rinkSize">Rink size to filter by (optional).</param>
        /// <param name="facility">Facility to filter by (optional).</param>
        /// <returns>A list of monthly booking reports with flag status.</returns>
        [HttpGet("GetMonthlyReport")]
        public async Task<IActionResult> GetMonthlyReport(
            [FromQuery] string? sport,
            [FromQuery] string? city,
            [FromQuery] string? rinkSize,
            [FromQuery] string? facility)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _statsRepository.GetMonthlyReportAsync(sport, city, rinkSize, facility);
                stopwatch.Stop();
                _logger.LogInformation($"GetMonthlyReport completed in {stopwatch.ElapsedMilliseconds}ms - {result.Count} monthly reports");
                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetMonthlyReport failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        /// <summary>
        /// Get sport comparison report by city.
        /// </summary>
        /// <param name="city">City to filter by (optional).</param>
        /// <param name="month">Month to filter by (optional).</param>
        /// <returns>A list of sport comparison results with flag status.</returns>
        [HttpGet("GetSportComparison")]
        public async Task<IActionResult> GetSportComparison(
            [FromQuery] string? city,
            [FromQuery] int? month)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _statsRepository.GetSportComparisonReportAsync(city, month);
                stopwatch.Stop();
                _logger.LogInformation($"GetSportComparison completed in {stopwatch.ElapsedMilliseconds}ms - {result.Count} sport comparisons");
                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetSportComparison failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        // --- Filtros dinámicos para selects ---
        [HttpGet("GetSports")]
        public async Task<IActionResult> GetSports()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _statsRepository.GetSportsAsync();
                stopwatch.Stop();
                _logger.LogInformation($"GetSports completed in {stopwatch.ElapsedMilliseconds}ms - {result.Count} sports");
                return Ok(result.Select(x => new { value = x }));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetSports failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        [HttpGet("GetCities")]
        public async Task<IActionResult> GetCities()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _statsRepository.GetCitiesAsync();
                stopwatch.Stop();
                _logger.LogInformation($"GetCities completed in {stopwatch.ElapsedMilliseconds}ms - {result.Count} cities");
                return Ok(result.Select(x => new { value = x }));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetCities failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        [HttpGet("GetRinkSizes")]
        public async Task<IActionResult> GetRinkSizes()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _statsRepository.GetRinkSizesAsync();
                stopwatch.Stop();
                _logger.LogInformation($"GetRinkSizes completed in {stopwatch.ElapsedMilliseconds}ms - {result.Count} rink sizes");
                return Ok(result.Select(x => new { value = x }));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetRinkSizes failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        [HttpGet("GetFacilities")]
        public async Task<IActionResult> GetFacilities()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _statsRepository.GetFacilitiesAsync();
                stopwatch.Stop();
                _logger.LogInformation($"GetFacilities completed in {stopwatch.ElapsedMilliseconds}ms - {result.Count} facilities");
                return Ok(result.Select(x => new { value = x }));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetFacilities failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }
    }
}
