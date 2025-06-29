using CatchCornerStats.Core.Results;
using CatchCornerStats.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CatchCornerStats.Data.Repositories
{
    /// <summary>
    /// Optimized version of StatsRepository with improved query performance
    /// </summary>
    public class StatsRepositoryOptimized : IStatsRepository
    {
        private readonly AppDbContext _context;

        public StatsRepositoryOptimized(AppDbContext context)
        {
            _context = context;
        }

        public async Task<double> GetAverageLeadTimeAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities)
        {
            var query = BuildBaseQuery(sports, cities, rinkSizes, facilities);

            // OPTIMIZACIÓN: Calcular promedio directamente en SQL
            var result = await query
                .Where(x => x.CreatedDateUtc != null && x.HappeningDate != null)
                .Select(x => EF.Functions.DateDiffDay(x.CreatedDateUtc, x.HappeningDate))
                .AverageAsync();

            return result;
        }

        public async Task<Dictionary<string, double>> GetLeadTimeBreakdownAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
            var query = BuildBaseQuery(sports, cities, rinkSizes, facilities);
            ApplyDateFilters(ref query, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);

            // OPTIMIZACIÓN: Agregación en SQL en lugar de memoria
            var breakdown = await query
                .Where(x => x.CreatedDateUtc != null && x.HappeningDate != null)
                .Select(x => new
                {
                    LeadTimeDays = EF.Functions.DateDiffDay(x.CreatedDateUtc, x.HappeningDate)
                })
                .GroupBy(x => x.LeadTimeDays <= 30 ? x.LeadTimeDays.ToString() : "+30")
                .Select(g => new
                {
                    DaysInAdvance = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            if (!breakdown.Any()) return new Dictionary<string, double>();

            var totalCount = breakdown.Sum(x => x.Count);

            return breakdown
                .OrderBy(x => x.DaysInAdvance == "+30" ? 999 : int.Parse(x.DaysInAdvance))
                .ToDictionary(
                    x => x.DaysInAdvance,
                    x => (double)x.Count / totalCount * 100
                );
        }

        public async Task<Dictionary<string, double>> GetBookingsByDayAsync(string? sport, string? city, string? rinkSize, string? facility, int? month, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
            var query = BuildBaseQuery(sport, city, rinkSize, facility);
            ApplyDateFilters(ref query, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);

            if (month.HasValue)
                query = query.Where(x => x.HappeningDate.Month == month);

            // OPTIMIZACIÓN: Una sola consulta con agregación en SQL
            var result = await query
                .GroupBy(x => x.HappeningDate.DayOfWeek)
                .Select(g => new
                {
                    DayOfWeek = g.Key.ToString(),
                    UniqueBookings = g.Select(x => x.BookingNumber).Distinct().Count()
                })
                .ToListAsync();

            if (!result.Any()) return new Dictionary<string, double>();

            var totalBookings = result.Sum(x => x.UniqueBookings);

            return result.ToDictionary(
                x => x.DayOfWeek,
                x => (double)x.UniqueBookings / totalBookings * 100
            );
        }

        public async Task<BookingsByStartTimeResult> GetBookingsByStartTimeAsync(string? sport, string? city, string? rinkSize, string? facility, int? month, int? dayOfWeek, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
            var query = BuildBaseQuery(sport, city, rinkSize, facility);
            ApplyDateFilters(ref query, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);

            if (month.HasValue)
                query = query.Where(x => x.HappeningDate.Month == month);
            if (dayOfWeek.HasValue)
                query = query.Where(x => (int)x.HappeningDate.DayOfWeek == dayOfWeek.Value);

            // OPTIMIZACIÓN: Agregación en SQL
            var grouped = await query
                .GroupBy(x => x.StartTime.Hours)
                .Select(g => new
                {
                    StartHour = g.Key,
                    UniqueBookings = g.Select(x => x.BookingNumber).Distinct().Count()
                })
                .ToListAsync();

            var totalBookings = grouped.Sum(g => g.UniqueBookings);
            var data = grouped.ToDictionary(
                g => FormatHour(g.StartHour),
                g => g.UniqueBookings
            );

            return new BookingsByStartTimeResult
            {
                TotalBookings = totalBookings,
                Data = data
            };
        }

        public async Task<Dictionary<string, double>> GetBookingDurationBreakdownAsync(string? sport, string? city, string? rinkSize, string? facility, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
            var query = BuildBaseQuery(sport, city, rinkSize, facility);
            ApplyDateFilters(ref query, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);

            // OPTIMIZACIÓN: Calcular duración en SQL
            var durations = await query
                .Select(x => new
                {
                    x.BookingNumber,
                    DurationHours = Math.Round((x.EndTime - x.StartTime).TotalHours, 1)
                })
                .GroupBy(x => x.DurationHours)
                .Select(g => new
                {
                    Duration = $"{g.Key} hr",
                    UniqueBookings = g.Select(x => x.BookingNumber).Distinct().Count()
                })
                .ToListAsync();

            if (!durations.Any()) return new Dictionary<string, double>();

            var totalBookings = durations.Sum(x => x.UniqueBookings);

            return durations.ToDictionary(
                x => x.Duration,
                x => (double)x.UniqueBookings / totalBookings * 100
            );
        }

        public async Task<List<MonthlyReportResult>> GetMonthlyReportAsync(string? sport, string? city, string? rinkSize, string? facility)
        {
            var query = BuildBaseQuery(sport, city, rinkSize, facility);

            // OPTIMIZACIÓN: Agregación en SQL con cálculo de mes anterior
            var monthlyData = await query
                .GroupBy(x => new
                {
                    x.Facility,
                    Month = x.HappeningDate.Month,
                    Year = x.HappeningDate.Year
                })
                .Select(g => new
                {
                    FacilityName = g.Key.Facility,
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    TotalBookings = g.Select(x => x.BookingNumber).Distinct().Count()
                })
                .OrderBy(x => x.FacilityName)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            var report = new List<MonthlyReportResult>();

            foreach (var current in monthlyData)
            {
                // Calcular mes anterior de forma más eficiente
                var (prevMonth, prevYear) = GetPreviousMonth(current.Month, current.Year);
                
                var previousMonthBookings = monthlyData
                    .FirstOrDefault(x => x.FacilityName == current.FacilityName && 
                                       x.Month == prevMonth && 
                                       x.Year == prevYear)
                    ?.TotalBookings;

                var percentageDrop = previousMonthBookings.HasValue && previousMonthBookings.Value > 0
                    ? (double)(previousMonthBookings.Value - current.TotalBookings) / previousMonthBookings.Value * 100
                    : (double?)null;

                var isFlagged = current.TotalBookings >= 10 &&
                                previousMonthBookings.HasValue &&
                                percentageDrop.HasValue &&
                                percentageDrop.Value >= 50;

                report.Add(new MonthlyReportResult
                {
                    FacilityName = current.FacilityName,
                    MonthYear = $"{current.Month}/{current.Year}",
                    TotalBookings = current.TotalBookings,
                    PreviousMonthBookings = previousMonthBookings,
                    PercentageDrop = percentageDrop,
                    IsFlagged = isFlagged
                });
            }

            return report;
        }

        public async Task<List<SportComparisonResult>> GetSportComparisonReportAsync(string? city, int? month)
        {
            var query = BuildBaseQuery(null, city, null, null);

            if (month.HasValue)
                query = query.Where(x => x.HappeningDate.Month == month);

            // OPTIMIZACIÓN: Una sola consulta con ranking en SQL
            var sportBookings = await query
                .Where(x => !string.IsNullOrEmpty(x.Sport) && !string.IsNullOrEmpty(x.City))
                .GroupBy(x => new { x.Sport, x.City })
                .Select(g => new
                {
                    Sport = g.Key.Sport,
                    City = g.Key.City,
                    TotalBookings = g.Select(x => x.BookingNumber).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalBookings)
                .ToListAsync();

            if (!sportBookings.Any()) return new List<SportComparisonResult>();

            var maxBookings = sportBookings.First().TotalBookings;
            var top6Sports = sportBookings.Take(6).Select(x => x.Sport).ToHashSet();
            var top8Sports = sportBookings.Take(8).Select(x => x.Sport).ToHashSet();

            return sportBookings.Select(booking => new SportComparisonResult
            {
                Sport = booking.Sport,
                City = booking.City,
                TotalBookings = booking.TotalBookings,
                IsFlaggedTop6 = !top6Sports.Contains(booking.Sport) && 
                               sportBookings.Take(6).Any(s => s.TotalBookings < booking.TotalBookings),
                IsFlaggedTop8 = !top8Sports.Contains(booking.Sport) && 
                               sportBookings.Take(8).Any(s => s.TotalBookings < booking.TotalBookings),
                IsFlaggedHighBookings = booking.TotalBookings >= 60 || 
                                       (maxBookings > 0 && booking.TotalBookings >= maxBookings * 0.05)
            }).ToList();
        }

        public async Task<List<string>> GetSportsAsync()
        {
            return await _context.Arenas
                .Where(x => !string.IsNullOrEmpty(x.Sport))
                .Select(x => x.Sport)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        public async Task<List<string>> GetCitiesAsync()
        {
            return await _context.Arenas
                .Where(x => !string.IsNullOrEmpty(x.Area))
                .Select(x => x.Area)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        public async Task<List<string>> GetRinkSizesAsync()
        {
            return await _context.Arenas
                .Where(x => !string.IsNullOrEmpty(x.Size))
                .Select(x => x.Size)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        public async Task<List<string>> GetFacilitiesAsync()
        {
            return await _context.Arenas
                .Where(x => !string.IsNullOrEmpty(x.Facility))
                .Select(x => x.Facility)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        public async Task<List<StatsRawDto>> GetAllStatsRawAsync()
        {
            return await _context.Bookings
                .Join(_context.Arenas, 
                      b => b.FacilityId, 
                      a => a.FacilityId, 
                      (b, a) => new StatsRawDto
                      {
                          BookingNumber = b.BookingNumber,
                          CreatedDateUtc = b.CreatedDateUtc,
                          HappeningDate = b.HappeningDate,
                          StartTime = b.StartTime,
                          EndTime = b.EndTime,
                          Sport = a.Sport,
                          City = a.Area,
                          RinkSize = a.Size,
                          Facility = b.Facility
                      })
                .ToListAsync();
        }

        public async Task<(double averageLeadTime, int totalBookings)> GetAverageLeadTimeWithCountAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
            var query = BuildBaseQuery(sports, cities, rinkSizes, facilities);
            ApplyDateFilters(ref query, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);

            // OPTIMIZACIÓN: Una sola consulta para ambos valores
            var result = await query
                .Where(x => x.CreatedDateUtc != null && x.HappeningDate != null)
                .Select(x => new
                {
                    LeadTimeDays = EF.Functions.DateDiffDay(x.CreatedDateUtc, x.HappeningDate),
                    x.BookingNumber
                })
                .GroupBy(x => 1) // Agrupar todo para agregación
                .Select(g => new
                {
                    AverageLeadTime = g.Average(x => x.LeadTimeDays),
                    TotalBookings = g.Select(x => x.BookingNumber).Distinct().Count()
                })
                .FirstOrDefaultAsync();

            return result != null ? (result.AverageLeadTime, result.TotalBookings) : (0, 0);
        }

        #region Helper Methods

        private IQueryable<BookingQueryResult> BuildBaseQuery(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities)
        {
            var query = from b in _context.Bookings
                        join a in _context.Arenas on b.FacilityId equals a.FacilityId
                        select new BookingQueryResult
                        {
                            BookingNumber = b.BookingNumber,
                            CreatedDateUtc = b.CreatedDateUtc,
                            HappeningDate = b.HappeningDate,
                            StartTime = b.StartTime,
                            EndTime = b.EndTime,
                            Facility = b.Facility,
                            Sport = a.Sport,
                            City = a.Area,
                            RinkSize = a.Size
                        };

            if (sports?.Any(x => !string.IsNullOrWhiteSpace(x)) == true)
                query = query.Where(x => sports.Contains(x.Sport));

            if (cities?.Any(x => !string.IsNullOrWhiteSpace(x)) == true)
                query = query.Where(x => cities.Contains(x.City));

            if (rinkSizes?.Any(x => !string.IsNullOrWhiteSpace(x)) == true)
                query = query.Where(x => rinkSizes.Contains(x.RinkSize));

            if (facilities?.Any(x => !string.IsNullOrWhiteSpace(x)) == true)
                query = query.Where(x => facilities.Contains(x.Facility));

            return query;
        }

        private IQueryable<BookingQueryResult> BuildBaseQuery(string? sport, string? city, string? rinkSize, string? facility)
        {
            var query = from b in _context.Bookings
                        join a in _context.Arenas on b.FacilityId equals a.FacilityId
                        select new BookingQueryResult
                        {
                            BookingNumber = b.BookingNumber,
                            CreatedDateUtc = b.CreatedDateUtc,
                            HappeningDate = b.HappeningDate,
                            StartTime = b.StartTime,
                            EndTime = b.EndTime,
                            Facility = b.Facility,
                            Sport = a.Sport,
                            City = a.Area,
                            RinkSize = a.Size
                        };

            if (!string.IsNullOrEmpty(sport))
                query = query.Where(x => x.Sport == sport);

            if (!string.IsNullOrEmpty(city))
                query = query.Where(x => x.City == city);

            if (!string.IsNullOrEmpty(rinkSize))
                query = query.Where(x => x.RinkSize == rinkSize);

            if (!string.IsNullOrEmpty(facility))
                query = query.Where(x => x.Facility == facility);

            return query;
        }

        private void ApplyDateFilters(ref IQueryable<BookingQueryResult> query, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
            if (createdDateFrom.HasValue)
                query = query.Where(x => x.CreatedDateUtc >= createdDateFrom.Value);
            if (createdDateTo.HasValue)
                query = query.Where(x => x.CreatedDateUtc <= createdDateTo.Value);
            if (happeningDateFrom.HasValue)
                query = query.Where(x => x.HappeningDate >= happeningDateFrom.Value);
            if (happeningDateTo.HasValue)
                query = query.Where(x => x.HappeningDate <= happeningDateTo.Value);
        }

        private string FormatHour(int hour)
        {
            string ampm = hour < 12 ? "AM" : "PM";
            int hour12 = hour % 12 == 0 ? 12 : hour % 12;
            return $"{hour12}:00 {ampm}";
        }

        private (int month, int year) GetPreviousMonth(int currentMonth, int currentYear)
        {
            if (currentMonth == 1)
                return (12, currentYear - 1);
            return (currentMonth - 1, currentYear);
        }

        #endregion
    }

    /// <summary>
    /// Helper class for strongly-typed query results
    /// </summary>
    public class BookingQueryResult
    {
        public int BookingNumber { get; set; }
        public DateTime CreatedDateUtc { get; set; }
        public DateTime HappeningDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Facility { get; set; } = string.Empty;
        public string Sport { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string RinkSize { get; set; } = string.Empty;
    }
} 