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
        
        // Cache en memoria para filtros
        private static readonly Dictionary<string, (List<string> Data, DateTime Expiry)> _filterCache = new();
        private static readonly object _cacheLock = new object();
        private const int CACHE_DURATION_MINUTES = 30;

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

        // Método helper para obtener datos del cache
        private List<string> GetCachedFilterData(string cacheKey, Func<Task<List<string>>> dataLoader)
        {
            lock (_cacheLock)
            {
                if (_filterCache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
                {
                    _logger.LogInformation($"Cache hit for {cacheKey}: {cached.Data.Count} items");
                    return cached.Data;
                }
            }
            
            return null;
        }

        // Método helper para guardar datos en cache
        private void SetCachedFilterData(string cacheKey, List<string> data)
        {
            lock (_cacheLock)
            {
                _filterCache[cacheKey] = (data, DateTime.UtcNow.AddMinutes(CACHE_DURATION_MINUTES));
                _logger.LogInformation($"Cache set for {cacheKey}: {data.Count} items");
            }
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
                _logger.LogInformation("=== GetAverageLeadTime START ===");
                _logger.LogInformation("Received parameters:");
                _logger.LogInformation($"  sports: [{string.Join(", ", sports ?? new List<string>())}] (null: {sports == null})");
                _logger.LogInformation($"  cities: [{string.Join(", ", cities ?? new List<string>())}] (null: {cities == null})");
                _logger.LogInformation($"  rinkSizes: [{string.Join(", ", rinkSizes ?? new List<string>())}] (null: {rinkSizes == null})");
                _logger.LogInformation($"  facilities: [{string.Join(", ", facilities ?? new List<string>())}] (null: {facilities == null})");
                _logger.LogInformation($"  createdDateFrom: {createdDateFrom} (null: {createdDateFrom == null})");
                _logger.LogInformation($"  createdDateTo: {createdDateTo} (null: {createdDateTo == null})");
                _logger.LogInformation($"  happeningDateFrom: {happeningDateFrom} (null: {happeningDateFrom == null})");
                _logger.LogInformation($"  happeningDateTo: {happeningDateTo} (null: {happeningDateTo == null})");

                var (averageLeadTime, totalBookings) = await _statsRepository.GetAverageLeadTimeWithCountAsync(sports, cities, rinkSizes, facilities, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);
                stopwatch.Stop();
                
                _logger.LogInformation($"Repository returned: AverageLeadTime = {averageLeadTime:F2} days, TotalBookings = {totalBookings}");
                _logger.LogInformation($"GetAverageLeadTime completed in {stopwatch.ElapsedMilliseconds}ms - Result: {averageLeadTime:F2} days, {totalBookings} bookings");
                _logger.LogInformation("=== GetAverageLeadTime END ===");
                
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
        /// Get the percentage distribution of bookings by start time.
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
        /// <returns>Un objeto con totalBookings y data (diccionario de hora a cantidad).</returns>
        [HttpGet("GetBookingsByStartTime")]
        public async Task<IActionResult> GetBookingsByStartTime(
            [FromQuery] List<string>? sports,
            [FromQuery] List<string>? cities,
            [FromQuery] List<string>? rinkSizes,
            [FromQuery] List<string>? facilities,
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
                _logger.LogInformation("=== GetBookingsByStartTime START ===");
                _logger.LogInformation("Received parameters:");
                _logger.LogInformation($"  sports: [{string.Join(", ", sports ?? new List<string>())}] (null: {sports == null})");
                _logger.LogInformation($"  cities: [{string.Join(", ", cities ?? new List<string>())}] (null: {cities == null})");
                _logger.LogInformation($"  rinkSizes: [{string.Join(", ", rinkSizes ?? new List<string>())}] (null: {rinkSizes == null})");
                _logger.LogInformation($"  facilities: [{string.Join(", ", facilities ?? new List<string>())}] (null: {facilities == null})");
                _logger.LogInformation($"  month: {month} (null: {month == null})");
                _logger.LogInformation($"  dayOfWeek: {dayOfWeek} (null: {dayOfWeek == null})");
                _logger.LogInformation($"  createdDateFrom: {createdDateFrom} (null: {createdDateFrom == null})");
                _logger.LogInformation($"  createdDateTo: {createdDateTo} (null: {createdDateTo == null})");
                _logger.LogInformation($"  happeningDateFrom: {happeningDateFrom} (null: {happeningDateFrom == null})");
                _logger.LogInformation($"  happeningDateTo: {happeningDateTo} (null: {happeningDateTo == null})");

                var result = await _statsRepository.GetBookingsByStartTimeAsync(sports, cities, rinkSizes, facilities, month, dayOfWeek, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);
                stopwatch.Stop();
                
                _logger.LogInformation($"Repository returned: TotalBookings = {result.TotalBookings}, TimeSlots = {result.Data.Count}");
                _logger.LogInformation($"GetBookingsByStartTime completed in {stopwatch.ElapsedMilliseconds}ms - {result.TotalBookings} total bookings, {result.Data.Count} time slots");
                _logger.LogInformation("=== GetBookingsByStartTime END ===");
                
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
                var result = await _statsRepository.GetBookingDurationBreakdownAsync(sports, cities, rinkSizes, facilities, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);
                stopwatch.Stop();
                _logger.LogInformation($"GetBookingDurationBreakdown completed in {stopwatch.ElapsedMilliseconds}ms - {result.Data.Count} start time categories");
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
                _logger.LogInformation($"GetMonthlyReport completed in {stopwatch.ElapsedMilliseconds}ms - {result.Facilities.Count} facilities, {result.FlaggedFacilities.Count} flagged");
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
                // Intentar obtener del cache primero
                var cachedData = GetCachedFilterData("sports", () => _statsRepository.GetSportsAsync());
                if (cachedData != null)
                {
                    stopwatch.Stop();
                    _logger.LogInformation($"GetSports completed from cache in {stopwatch.ElapsedMilliseconds}ms - {cachedData.Count} sports");
                    return Ok(cachedData.Select(x => new { value = x }));
                }

                // Si no está en cache, cargar desde la base de datos
                var result = await _statsRepository.GetSportsAsync();
                
                // Guardar en cache
                SetCachedFilterData("sports", result);
                
                stopwatch.Stop();
                _logger.LogInformation($"GetSports completed from DB in {stopwatch.ElapsedMilliseconds}ms - {result.Count} sports");
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
                // Intentar obtener del cache primero
                var cachedData = GetCachedFilterData("cities", () => _statsRepository.GetCitiesAsync());
                if (cachedData != null)
                {
                    stopwatch.Stop();
                    _logger.LogInformation($"GetCities completed from cache in {stopwatch.ElapsedMilliseconds}ms - {cachedData.Count} cities");
                    return Ok(cachedData.Select(x => new { value = x }));
                }

                // Si no está en cache, cargar desde la base de datos
                var result = await _statsRepository.GetCitiesAsync();
                
                // Guardar en cache
                SetCachedFilterData("cities", result);
                
                stopwatch.Stop();
                _logger.LogInformation($"GetCities completed from DB in {stopwatch.ElapsedMilliseconds}ms - {result.Count} cities");
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
                // Intentar obtener del cache primero
                var cachedData = GetCachedFilterData("rinkSizes", () => _statsRepository.GetRinkSizesAsync());
                if (cachedData != null)
                {
                    stopwatch.Stop();
                    _logger.LogInformation($"GetRinkSizes completed from cache in {stopwatch.ElapsedMilliseconds}ms - {cachedData.Count} rink sizes");
                    return Ok(cachedData.Select(x => new { value = x }));
                }

                // Si no está en cache, cargar desde la base de datos
                var result = await _statsRepository.GetRinkSizesAsync();
                
                // Guardar en cache
                SetCachedFilterData("rinkSizes", result);
                
                stopwatch.Stop();
                _logger.LogInformation($"GetRinkSizes completed from DB in {stopwatch.ElapsedMilliseconds}ms - {result.Count} rink sizes");
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
                // Intentar obtener del cache primero
                var cachedData = GetCachedFilterData("facilities", () => _statsRepository.GetFacilitiesAsync());
                if (cachedData != null)
                {
                    stopwatch.Stop();
                    _logger.LogInformation($"GetFacilities completed from cache in {stopwatch.ElapsedMilliseconds}ms - {cachedData.Count} facilities");
                    return Ok(cachedData.Select(x => new { value = x }));
                }

                // Si no está en cache, cargar desde la base de datos
                var result = await _statsRepository.GetFacilitiesAsync();
                
                // Guardar en cache
                SetCachedFilterData("facilities", result);
                
                stopwatch.Stop();
                _logger.LogInformation($"GetFacilities completed from DB in {stopwatch.ElapsedMilliseconds}ms - {result.Count} facilities");
                return Ok(result.Select(x => new { value = x }));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetFacilities failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        /// <summary>
        /// Get all filter options in a single request (optimized)
        /// </summary>
        [HttpGet("GetAllFilters")]
        public async Task<IActionResult> GetAllFilters()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Intentar obtener del cache primero
                var cachedSports = GetCachedFilterData("sports", () => _statsRepository.GetSportsAsync());
                var cachedCities = GetCachedFilterData("cities", () => _statsRepository.GetCitiesAsync());
                var cachedRinkSizes = GetCachedFilterData("rinkSizes", () => _statsRepository.GetRinkSizesAsync());
                var cachedFacilities = GetCachedFilterData("facilities", () => _statsRepository.GetFacilitiesAsync());

                if (cachedSports != null && cachedCities != null && cachedRinkSizes != null && cachedFacilities != null)
                {
                    stopwatch.Stop();
                    _logger.LogInformation($"GetAllFilters completed from cache in {stopwatch.ElapsedMilliseconds}ms");
                    return Ok(new
                    {
                        sports = cachedSports.Select(x => new { value = x }),
                        cities = cachedCities.Select(x => new { value = x }),
                        rinkSizes = cachedRinkSizes.Select(x => new { value = x }),
                        facilities = cachedFacilities.Select(x => new { value = x })
                    });
                }

                // Si no están en cache, cargar desde la base de datos SECUENCIALMENTE para evitar problemas de concurrencia
                var sports = await _statsRepository.GetSportsAsync();
                var cities = await _statsRepository.GetCitiesAsync();
                var rinkSizes = await _statsRepository.GetRinkSizesAsync();
                var facilities = await _statsRepository.GetFacilitiesAsync();

                // Guardar en cache
                SetCachedFilterData("sports", sports);
                SetCachedFilterData("cities", cities);
                SetCachedFilterData("rinkSizes", rinkSizes);
                SetCachedFilterData("facilities", facilities);

                stopwatch.Stop();
                _logger.LogInformation($"GetAllFilters completed from DB in {stopwatch.ElapsedMilliseconds}ms - Sports: {sports.Count}, Cities: {cities.Count}, RinkSizes: {rinkSizes.Count}, Facilities: {facilities.Count}");
                
                return Ok(new
                {
                    sports = sports.Select(x => new { value = x }),
                    cities = cities.Select(x => new { value = x }),
                    rinkSizes = rinkSizes.Select(x => new { value = x }),
                    facilities = facilities.Select(x => new { value = x })
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"GetAllFilters failed after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        [HttpGet("GetBookingsByDayReport")]
        public async Task<IActionResult> GetBookingsByDayReport(
            [FromQuery] List<string>? sports,
            [FromQuery] List<string>? cities,
            [FromQuery] List<string>? rinkSizes,
            [FromQuery] List<string>? facilities,
            [FromQuery] int? month,
            [FromQuery] int? year,
            [FromQuery] DateTime? createdDateFrom,
            [FromQuery] DateTime? createdDateTo,
            [FromQuery] DateTime? happeningDateFrom,
            [FromQuery] DateTime? happeningDateTo)
        {
            try
            {
                _logger.LogInformation("=== GetBookingsByDayReport START ===");
                _logger.LogInformation("Received parameters:");
                _logger.LogInformation($"  sports: [{string.Join(", ", sports ?? new List<string>())}] (null: {sports == null})");
                _logger.LogInformation($"  cities: [{string.Join(", ", cities ?? new List<string>())}] (null: {cities == null})");
                _logger.LogInformation($"  rinkSizes: [{string.Join(", ", rinkSizes ?? new List<string>())}] (null: {rinkSizes == null})");
                _logger.LogInformation($"  facilities: [{string.Join(", ", facilities ?? new List<string>())}] (null: {facilities == null})");
                _logger.LogInformation($"  month: {month} (null: {month == null})");
                _logger.LogInformation($"  year: {year} (null: {year == null})");
                _logger.LogInformation($"  createdDateFrom: {createdDateFrom} (null: {createdDateFrom == null})");
                _logger.LogInformation($"  createdDateTo: {createdDateTo} (null: {createdDateTo == null})");
                _logger.LogInformation($"  happeningDateFrom: {happeningDateFrom} (null: {happeningDateFrom == null})");
                _logger.LogInformation($"  happeningDateTo: {happeningDateTo} (null: {happeningDateTo == null})");

                var result = await _statsRepository.GetBookingsByDayReportAsync(
                    sports, cities, rinkSizes, facilities, month, year,
                    createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);
                
                _logger.LogInformation($"Repository returned {result?.Count ?? 0} results");
                if (result?.Any() == true)
                {
                    _logger.LogInformation("Sample results:");
                    foreach (var item in result.Take(3))
                    {
                        _logger.LogInformation($"  {item.DayOfWeek}: {item.BookingsCount} bookings ({item.Percentage}%)");
                    }
                }
                
                _logger.LogInformation("=== GetBookingsByDayReport END ===");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetBookingsByDayReport: {Message}", ex.Message);
                return StatusCode(500, new { error = "Error al obtener el reporte de bookings por día.", details = ex.Message });
            }
        }
    }
}
