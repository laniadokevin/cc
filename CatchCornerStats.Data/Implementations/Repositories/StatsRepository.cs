using CatchCornerStats.Core.Results;
using CatchCornerStats.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace CatchCornerStats.Data.Repositories
{
    /// <summary>
    /// Optimized version of StatsRepository with improved query performance
    /// </summary>
    public class StatsRepository : IStatsRepository
    {
        private readonly AppDbContext _context;

        public StatsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<double> GetAverageLeadTimeAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities)
        {
            var query = BuildBaseQuery(sports, cities, rinkSizes, facilities);

            // OPTIMIZACIÓN: Calcular promedio directamente en SQL
            var result = await query
                .Where(x => x.CreatedDateUtc != null)
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
                .Where(x => x.CreatedDateUtc != null)
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
            // Forzar filtro de mes: si no se especifica, usar el mes actual
            if (!month.HasValue)
            {
                month = DateTime.UtcNow.Month;
            }
            int currentYear = DateTime.UtcNow.Year;

            var query = from b in _context.Bookings
                        join a in _context.Arenas on b.FacilityId equals a.FacilityId
                        select new
                        {
                            b.HappeningDate,
                            a.Sport,
                            a.Area,
                            a.Size,
                            b.Facility,
                            b.BookingNumber,
                            b.CreatedDateUtc
                        };

            if (!string.IsNullOrEmpty(sport))
                query = query.Where(x => x.Sport == sport);

            if (!string.IsNullOrEmpty(city))
                query = query.Where(x => x.Area == city);

            if (!string.IsNullOrEmpty(rinkSize))
                query = query.Where(x => x.Size == rinkSize);

            if (!string.IsNullOrEmpty(facility))
                query = query.Where(x => x.Facility == facility);

            // Filtros de fecha
            if (createdDateFrom.HasValue)
                query = query.Where(x => x.CreatedDateUtc >= createdDateFrom.Value);
            if (createdDateTo.HasValue)
                query = query.Where(x => x.CreatedDateUtc <= createdDateTo.Value);
            if (happeningDateFrom.HasValue)
                query = query.Where(x => x.HappeningDate >= happeningDateFrom.Value);
            if (happeningDateTo.HasValue)
                query = query.Where(x => x.HappeningDate <= happeningDateTo.Value);

            // Trae los datos a memoria y elimina duplicados por BookingNumber
            var bookings = await query
                .GroupBy(x => x.BookingNumber)
                .Select(g => g.First())
                .ToListAsync();

            var totalBookings = bookings.Select(x => x.BookingNumber).Distinct().Count();

            if (totalBookings == 0) return new Dictionary<string, double>();

            var result = bookings
                .GroupBy(x => x.HappeningDate.DayOfWeek)
                .Select(g => new
                {
                    DayOfWeek = g.Key.ToString(),
                    UniqueBookings = g.Count(),
                    Percentage = (double)g.Count() / totalBookings * 100
                })
                .ToDictionary(x => x.DayOfWeek, x => x.Percentage);

            return result;
        }

        public async Task<BookingsByStartTimeResult> GetBookingsByStartTimeAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities, int? month, int? dayOfWeek, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
            var query = BuildBaseQuery(sports, cities, rinkSizes, facilities);
            ApplyDateFilters(ref query, createdDateFrom, createdDateTo, happeningDateFrom, happeningDateTo);

            // Aplicar filtros adicionales si se especifican
            if (month.HasValue)
                query = query.Where(x => x.HappeningDate.Month == month.Value);
            
            if (dayOfWeek.HasValue)
                query = query.Where(x => (int)x.HappeningDate.DayOfWeek == dayOfWeek.Value);

            // OPTIMIZACIÓN: Agrupar directamente por hora de inicio
            var grouped = await query
                .GroupBy(x => x.StartTime.Value.Hours)
                .Select(g => new
                {
                    StartHour = g.Key,
                    UniqueBookings = g.Select(x => x.BookingNumber).Distinct().Count()
                })
                .OrderBy(x => x.StartHour)
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

        public async Task<BookingDurationBreakdownResult> GetBookingDurationBreakdownAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
            // Usar BuildBaseQuery para poder filtrar por rinkSizes (JOIN con Arena)
            var query = BuildBaseQuery(sports, null, rinkSizes, facilities);
            if (createdDateFrom.HasValue)
                query = query.Where(x => x.CreatedDateUtc >= createdDateFrom.Value);
            if (createdDateTo.HasValue)
                query = query.Where(x => x.CreatedDateUtc <= createdDateTo.Value);
            if (happeningDateFrom.HasValue)
                query = query.Where(x => x.HappeningDate >= happeningDateFrom.Value);
            if (happeningDateTo.HasValue)
                query = query.Where(x => x.HappeningDate <= happeningDateTo.Value);

            // Traer solo BookingNumber, StartTime y EndTime únicos
            var bookings = await query
                       .Where(x => x.StartTime != null && x.EndTime != null)
                       .Select(x => new { x.BookingNumber, x.StartTime, x.EndTime })
                       .Distinct()
                       .ToListAsync();
            
            var totalBookings = bookings.Count();

            if (!bookings.Any()) return new BookingDurationBreakdownResult { TotalBookings = 0, Data = new Dictionary<string, int>() };

            var durations = bookings
                .GroupBy(x => Math.Round((x.EndTime.Value - x.StartTime.Value).TotalHours, 1))
                .Where(g => g.Key > 0)
                .Select(g => new
                {
                    Duration = $"{g.Key} hr",
                    UniqueBookings = g.Count()
                })
                .OrderBy(x => x.Duration)
                .ToList();

            if (totalBookings == 0) return new BookingDurationBreakdownResult { TotalBookings = 0, Data = new Dictionary<string, int>() };

            var avgDuration = bookings
                .Select(x => (x.EndTime.Value - x.StartTime.Value).TotalHours)
                .Where(d => d > 0)
                .DefaultIfEmpty(0)
                .Average();

            return new BookingDurationBreakdownResult {
                TotalBookings = totalBookings,
                Data = durations.ToDictionary(x => x.Duration, x => x.UniqueBookings),
                AverageDuration = avgDuration
            };
        }

        public async Task<MonthlyReportResponseDto> GetMonthlyReportAsync(string? sport, string? city, string? rinkSize, string? facility)
        {
            var query = BuildBaseQuery(sport, city, rinkSize, facility);

            var monthlyData = await query
                .GroupBy(x => new { x.Facility, x.HappeningDate.Year, x.HappeningDate.Month, x.BookingNumber })
                .Select(g => new
                {
                    FacilityName = g.Key.Facility,
                    g.Key.Year,
                    g.Key.Month
                })
                .GroupBy(x => new { x.FacilityName, x.Year, x.Month })
                .Select(g => new
                {
                    FacilityName = g.Key.FacilityName,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalBookings = g.Count()
                })
                .OrderBy(x => x.FacilityName)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Construir tabla dinámica
            var facilitiesDict = new Dictionary<string, MonthlyReportFacilityDto>();
            var allMonths = new HashSet<string>();
            foreach (var row in monthlyData)
            {
                var monthYear = $"{row.Month:D2}/{row.Year}";
                allMonths.Add(monthYear);
                if (!facilitiesDict.TryGetValue(row.FacilityName, out var facilityDto))
                {
                    facilityDto = new MonthlyReportFacilityDto { FacilityName = row.FacilityName };
                    facilitiesDict[row.FacilityName] = facilityDto;
                }
                facilityDto.MonthlyBookings[monthYear] = row.TotalBookings;
            }

            // Flags
            var flagged = new List<MonthlyReportFlaggedDto>();
            foreach (var facilityDto in facilitiesDict.Values)
            {
                var monthsOrdered = facilityDto.MonthlyBookings.Keys.OrderBy(m => m).ToList();
                for (int i = 1; i < monthsOrdered.Count; i++)
                {
                    var prevMonth = monthsOrdered[i - 1];
                    var currMonth = monthsOrdered[i];
                    var prevBookings = facilityDto.MonthlyBookings[prevMonth];
                    var currBookings = facilityDto.MonthlyBookings[currMonth];
                    if (prevBookings >= 10)
                    {
                        var drop = prevBookings > 0 ? (double)(prevBookings - currBookings) / prevBookings * 100 : 0;
                        if (drop >= 50)
                        {
                            flagged.Add(new MonthlyReportFlaggedDto
                            {
                                FacilityName = facilityDto.FacilityName,
                                MonthYear = currMonth,
                                PreviousMonthBookings = prevBookings,
                                CurrentMonthBookings = currBookings,
                                PercentageDrop = drop
                            });
                        }
                    }
                }
            }

            // Normalizar: asegurar que todos los meses estén presentes en cada facility (con 0 si falta)
            var allMonthsOrdered = allMonths.OrderBy(m => m).ToList();
            foreach (var facilityDto in facilitiesDict.Values)
            {
                foreach (var month in allMonthsOrdered)
                {
                    if (!facilityDto.MonthlyBookings.ContainsKey(month))
                        facilityDto.MonthlyBookings[month] = 0;
                }
                // Ordenar el diccionario por mes
                facilityDto.MonthlyBookings = facilityDto.MonthlyBookings.OrderBy(kv => kv.Key)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            return new MonthlyReportResponseDto
            {
                Facilities = facilitiesDict.Values.ToList(),
                FlaggedFacilities = flagged
            };
        }

        public async Task<List<SportComparisonResult>> GetSportComparisonReportAsync(string? city, int? month)
        {
            var query = BuildBaseQuery(null, city, null, null);

            if (month.HasValue)
                query = query.Where(x => x.HappeningDate.Month == month);

            // OPTIMIZACIÓN: Una sola consulta con ranking por ciudad
            var sportBookings = await query
                .Where(x => !string.IsNullOrEmpty(x.Sport) && !string.IsNullOrEmpty(x.City))
                .GroupBy(x => new { x.Sport, x.City })
                .Select(g => new
                {
                    Sport = g.Key.Sport,
                    City = g.Key.City,
                    TotalBookings = g.Select(x => x.BookingNumber).Distinct().Count()
                })
                .ToListAsync();

            if (!sportBookings.Any()) return new List<SportComparisonResult>();

            // Agrupar por ciudad y calcular rankings y flags por ciudad
            var results = new List<SportComparisonResult>();
            
            var cities = sportBookings.Select(x => x.City).Distinct();
            
            foreach (var currentCity in cities)
            {
                var citySports = sportBookings
                    .Where(x => x.City == currentCity)
                    .OrderByDescending(x => x.TotalBookings)
                    .ToList();

                if (!citySports.Any()) continue;

                var maxBookings = citySports.First().TotalBookings;
                
                // Calcular rankings por ciudad
                for (int i = 0; i < citySports.Count; i++)
                {
                    var sport = citySports[i];
                    var ranking = i + 1; // Ranking basado en posición (1 = más popular)
                    
                    // Determinar flags según criterios específicos
                    bool isFlaggedTop6 = false;
                    bool isFlaggedTop8 = false;
                    bool isFlaggedHighBookings = false;

                    // Flag Top6: Si no está en top 6 pero tiene más bookings que un top 6
                    if (ranking > 6)
                    {
                        var top6MinBookings = citySports.Take(6).Min(x => x.TotalBookings);
                        isFlaggedTop6 = sport.TotalBookings > top6MinBookings;
                    }

                    // Flag Top8: Si no está en top 8 pero tiene más bookings que un top 8
                    if (ranking > 8)
                    {
                        var top8MinBookings = citySports.Take(8).Min(x => x.TotalBookings);
                        isFlaggedTop8 = sport.TotalBookings > top8MinBookings;
                    }

                    // Flag HighBookings: Si está rankeado 9+ y tiene 60+ bookings o 5% del más popular
                    if (ranking >= 9)
                    {
                        var threshold = Math.Max(60, maxBookings * 0.05);
                        isFlaggedHighBookings = sport.TotalBookings >= threshold;
                    }

                    results.Add(new SportComparisonResult
                    {
                        Sport = sport.Sport,
                        City = sport.City,
                        TotalBookings = sport.TotalBookings,
                        Ranking = ranking,
                        IsFlaggedTop6 = isFlaggedTop6,
                        IsFlaggedTop8 = isFlaggedTop8,
                        IsFlaggedHighBookings = isFlaggedHighBookings
                    });
                }
            }

            return results.OrderBy(x => x.City).ThenBy(x => x.Ranking).ToList();
        }

        public async Task<List<string>> GetSportsAsync()
        {
            // OPTIMIZACIÓN: Usar consulta más eficiente con índices
            return await _context.Arenas
                .Where(x => !string.IsNullOrEmpty(x.Sport))
                .Select(x => x.Sport)
                .Distinct()
                .OrderBy(x => x)
                .AsNoTracking() // OPTIMIZACIÓN: No tracking para consultas de solo lectura
                .ToListAsync();
        }

        public async Task<List<string>> GetCitiesAsync()
        {
            // OPTIMIZACIÓN: Usar consulta más eficiente con índices
            return await _context.Arenas
                .Where(x => !string.IsNullOrEmpty(x.Area))
                .Select(x => x.Area)
                .Distinct()
                .OrderBy(x => x)
                .AsNoTracking() // OPTIMIZACIÓN: No tracking para consultas de solo lectura
                .ToListAsync();
        }

        public async Task<List<string>> GetRinkSizesAsync()
        {
            // OPTIMIZACIÓN: Usar consulta más eficiente con índices
            return await _context.Arenas
                .Where(x => !string.IsNullOrEmpty(x.Size))
                .Select(x => x.Size)
                .Distinct()
                .OrderBy(x => x)
                .AsNoTracking() // OPTIMIZACIÓN: No tracking para consultas de solo lectura
                .ToListAsync();
        }

        public async Task<List<string>> GetFacilitiesAsync()
        {
            // OPTIMIZACIÓN: Usar consulta más eficiente con índices
            return await _context.Arenas
                .Where(x => !string.IsNullOrEmpty(x.Facility))
                .Select(x => x.Facility)
                .Distinct()
                .OrderBy(x => x)
                .AsNoTracking() // OPTIMIZACIÓN: No tracking para consultas de solo lectura
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
            // Log de entrada
            Console.WriteLine("=== StatsRepository.GetAverageLeadTimeWithCountAsync START ===");
            Console.WriteLine($"Parameters received:");
            Console.WriteLine($"  sports: [{string.Join(", ", sports ?? new List<string>())}] (null: {sports == null})");
            Console.WriteLine($"  cities: [{string.Join(", ", cities ?? new List<string>())}] (null: {cities == null})");
            Console.WriteLine($"  rinkSizes: [{string.Join(", ", rinkSizes ?? new List<string>())}] (null: {rinkSizes == null})");
            Console.WriteLine($"  facilities: [{string.Join(", ", facilities ?? new List<string>())}] (null: {facilities == null})");
            Console.WriteLine($"  createdDateFrom: {createdDateFrom} (null: {createdDateFrom == null})");
            Console.WriteLine($"  createdDateTo: {createdDateTo} (null: {createdDateTo == null})");
            Console.WriteLine($"  happeningDateFrom: {happeningDateFrom} (null: {happeningDateFrom == null})");
            Console.WriteLine($"  happeningDateTo: {happeningDateTo} (null: {happeningDateTo == null})");

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

            var averageLeadTime = result?.AverageLeadTime ?? 0;
            var totalBookings = result?.TotalBookings ?? 0;

            Console.WriteLine($"Query executed successfully. Result: AverageLeadTime = {averageLeadTime:F2} days, TotalBookings = {totalBookings}");
            Console.WriteLine("=== StatsRepository.GetAverageLeadTimeWithCountAsync END ===");

            return (averageLeadTime, totalBookings);
        }

        public async Task<List<BookingsByDayDto>> GetBookingsByDayReportAsync(
            List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities, int? month, int? year,
            DateTime? createdDateFrom = null, DateTime? createdDateTo = null, DateTime? happeningDateFrom = null, DateTime? happeningDateTo = null)
        {
            // Log de entrada
            Console.WriteLine("=== StatsRepository.GetBookingsByDayReportAsync START ===");
            Console.WriteLine($"Parameters received:");
            Console.WriteLine($"  sports: [{string.Join(", ", sports ?? new List<string>())}] (null: {sports == null})");
            Console.WriteLine($"  cities: [{string.Join(", ", cities ?? new List<string>())}] (null: {cities == null})");
            Console.WriteLine($"  rinkSizes: [{string.Join(", ", rinkSizes ?? new List<string>())}] (null: {rinkSizes == null})");
            Console.WriteLine($"  facilities: [{string.Join(", ", facilities ?? new List<string>())}] (null: {facilities == null})");
            Console.WriteLine($"  month: {month} (null: {month == null})");
            Console.WriteLine($"  year: {year} (null: {year == null})");
            Console.WriteLine($"  createdDateFrom: {createdDateFrom} (null: {createdDateFrom == null})");
            Console.WriteLine($"  createdDateTo: {createdDateTo} (null: {createdDateTo == null})");
            Console.WriteLine($"  happeningDateFrom: {happeningDateFrom} (null: {happeningDateFrom == null})");
            Console.WriteLine($"  happeningDateTo: {happeningDateTo} (null: {happeningDateTo == null})");

            var sql = @"
            WITH FilteredBookings AS (
                SELECT
                    b.[Booking Number] AS BookingNumber,
                    b.[Happening Date] AS HappeningDate,
                    a.[Sport],
                    a.[Area] AS City,
                    a.[Size] AS RinkSize,
                    b.[Facility]
                FROM [powerBI].[VW_Bookings] b
                INNER JOIN [powerBI].[VW_Arena] a ON b.[FacilityId] = a.[FacilityId]
                WHERE
                    (@sports IS NULL OR a.[Sport] IN (SELECT value FROM STRING_SPLIT(@sports, ',')))
                    AND (@cities IS NULL OR a.[Area] IN (SELECT value FROM STRING_SPLIT(@cities, ',')))
                    AND (@rinkSizes IS NULL OR a.[Size] IN (SELECT value FROM STRING_SPLIT(@rinkSizes, ',')))
                    AND (@facilities IS NULL OR b.[Facility] IN (SELECT value FROM STRING_SPLIT(@facilities, ',')))
                    AND (@month IS NULL OR MONTH(b.[Happening Date]) = @month)
                    AND (@year IS NULL OR YEAR(b.[Happening Date]) = @year)
                    AND (@createdDateFrom IS NULL OR b.[Created Date UTC] >= @createdDateFrom)
                    AND (@createdDateTo IS NULL OR b.[Created Date UTC] <= @createdDateTo)
                    AND (@happeningDateFrom IS NULL OR b.[Happening Date] >= @happeningDateFrom)
                    AND (@happeningDateTo IS NULL OR b.[Happening Date] <= @happeningDateTo)
            )
            , DayStats AS (
                SELECT
                    DATENAME(weekday, HappeningDate) AS DayOfWeek,
                    COUNT(DISTINCT BookingNumber) AS BookingsCount
                FROM FilteredBookings
                GROUP BY DATENAME(weekday, HappeningDate)
            )
            , TotalStats AS (
                SELECT SUM(BookingsCount) AS TotalBookings FROM DayStats
            )
            SELECT
                d.DayOfWeek,
                d.BookingsCount,
                t.TotalBookings,
                CAST(100.0 * d.BookingsCount / t.TotalBookings AS DECIMAL(5,2)) AS Percentage
            FROM DayStats d
            CROSS JOIN TotalStats t
            ORDER BY
                CASE d.DayOfWeek
                    WHEN 'Monday' THEN 1
                    WHEN 'Tuesday' THEN 2
                    WHEN 'Wednesday' THEN 3
                    WHEN 'Thursday' THEN 4
                    WHEN 'Friday' THEN 5
                    WHEN 'Saturday' THEN 6
                    WHEN 'Sunday' THEN 7
                END
            ";

            // Convertir listas a strings separados por comas
            var sportsParam = sports?.Any() == true ? string.Join(",", sports) : null;
            var citiesParam = cities?.Any() == true ? string.Join(",", cities) : null;
            var rinkSizesParam = rinkSizes?.Any() == true ? string.Join(",", rinkSizes) : null;
            var facilitiesParam = facilities?.Any() == true ? string.Join(",", facilities) : null;

            var parameters = new[]
            {
                new SqlParameter("@sports", (object?)sportsParam ?? DBNull.Value),
                new SqlParameter("@cities", (object?)citiesParam ?? DBNull.Value),
                new SqlParameter("@rinkSizes", (object?)rinkSizesParam ?? DBNull.Value),
                new SqlParameter("@facilities", (object?)facilitiesParam ?? DBNull.Value),
                new SqlParameter("@month", (object?)month ?? DBNull.Value),
                new SqlParameter("@year", (object?)year ?? DBNull.Value),
                new SqlParameter("@createdDateFrom", (object?)createdDateFrom ?? DBNull.Value),
                new SqlParameter("@createdDateTo", (object?)createdDateTo ?? DBNull.Value),
                new SqlParameter("@happeningDateFrom", (object?)happeningDateFrom ?? DBNull.Value),
                new SqlParameter("@happeningDateTo", (object?)happeningDateTo ?? DBNull.Value)
            };

            // Log de parámetros SQL
            Console.WriteLine("SQL Parameters:");
            foreach (var param in parameters)
            {
                Console.WriteLine($"  {param.ParameterName}: {param.Value} (Type: {param.Value?.GetType().Name ?? "DBNull"})");
            }

            // Log adicional para verificar valores específicos
            Console.WriteLine("Parameter verification:");
            Console.WriteLine($"  sports parameter: [{string.Join(", ", sports ?? new List<string>())}] -> SQL: {sportsParam}");
            Console.WriteLine($"  cities parameter: [{string.Join(", ", cities ?? new List<string>())}] -> SQL: {citiesParam}");
            Console.WriteLine($"  rinkSizes parameter: [{string.Join(", ", rinkSizes ?? new List<string>())}] -> SQL: {rinkSizesParam}");
            Console.WriteLine($"  facilities parameter: [{string.Join(", ", facilities ?? new List<string>())}] -> SQL: {facilitiesParam}");

            Console.WriteLine("Executing SQL query...");
            var result = await _context.Set<BookingsByDayDto>().FromSqlRaw(sql, parameters).ToListAsync();
            
            Console.WriteLine($"Query executed successfully. Returned {result.Count} results:");
            foreach (var item in result)
            {
                Console.WriteLine($"  {item.DayOfWeek}: {item.BookingsCount} bookings ({item.Percentage}%)");
            }
            
            Console.WriteLine("=== StatsRepository.GetBookingsByDayReportAsync END ===");
            return result;
        }

        public async Task<List<MonthlyReportGlobalDto>> GetMonthlyReportGlobalAsync(
            List<string>? sports,
            List<string>? cities,
            List<string>? rinkSizes,
            List<string>? facilities,
            DateTime? createdDateFrom,
            DateTime? createdDateTo,
            DateTime? happeningDateFrom,
            DateTime? happeningDateTo)
        {
            // Construir consulta base con filtros
            var query = from b in _context.Bookings
                        join a in _context.Arenas on b.FacilityId equals a.FacilityId
                        select new { b.BookingNumber, b.HappeningDate, a.Sport, a.Area, a.Size, a.Facility, b.CreatedDateUtc };

            if (sports?.Any() == true)
                query = query.Where(x => sports.Contains(x.Sport));
            if (cities?.Any() == true)
                query = query.Where(x => cities.Contains(x.Area));
            if (rinkSizes?.Any() == true)
                query = query.Where(x => rinkSizes.Contains(x.Size));
            if (facilities?.Any() == true)
                query = query.Where(x => facilities.Contains(x.Facility));
            if (createdDateFrom.HasValue)
                query = query.Where(x => x.CreatedDateUtc >= createdDateFrom.Value);
            if (createdDateTo.HasValue)
                query = query.Where(x => x.CreatedDateUtc <= createdDateTo.Value);
            if (happeningDateFrom.HasValue)
                query = query.Where(x => x.HappeningDate >= happeningDateFrom.Value);
            if (happeningDateTo.HasValue)
                query = query.Where(x => x.HappeningDate <= happeningDateTo.Value);

            // Hacer la agregación directamente en SQL para mejor rendimiento
            var monthlyData = await query
                .GroupBy(x => new { x.HappeningDate.Year, x.HappeningDate.Month, x.BookingNumber })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month
                })
                .GroupBy(x => new { x.Year, x.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalBookings = g.Count()
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();

            // Calcular PreviousMonthBookings y PercentageChange
            var result = new List<MonthlyReportGlobalDto>();
            for (int i = 0; i < monthlyData.Count; i++)
            {
                var current = monthlyData[i];
                var previous = i + 1 < monthlyData.Count ? monthlyData[i + 1] : null;
                decimal? percentageChange = null;
                if (previous != null && previous.TotalBookings != 0)
                {
                    percentageChange = ((current.TotalBookings - previous.TotalBookings) * 100.0m) / previous.TotalBookings;
                }
                result.Add(new MonthlyReportGlobalDto
                {
                    MonthYear = $"{current.Month:D2}/{current.Year}",
                    TotalBookings = current.TotalBookings,
                    PreviousMonthBookings = previous?.TotalBookings,
                    PercentageChange = percentageChange
                });
            }
            return result;
        }

        private int GetDayOrder(string dayOfWeek)
        {
            return dayOfWeek switch
            {
                "Monday" => 1,
                "Tuesday" => 2,
                "Wednesday" => 3,
                "Thursday" => 4,
                "Friday" => 5,
                "Saturday" => 6,
                "Sunday" => 7,
                _ => 8
            };
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

            // Aplicar filtros solo si las listas no son null y contienen elementos válidos
            if (sports?.Any(x => !string.IsNullOrWhiteSpace(x)) == true)
                query = query.Where(x => sports.Contains(x.Sport));

            if (cities?.Any(x => !string.IsNullOrWhiteSpace(x)) == true)
                query = query.Where(x => cities.Contains(x.City));

            if (rinkSizes != null && rinkSizes.Any())
            {
                var normalizedRinkSizes = rinkSizes.Select(r => r.Trim().ToLower()).ToList();
                query = query.Where(x => normalizedRinkSizes.Contains(x.RinkSize.Trim().ToLower()));
            }

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
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Facility { get; set; } = string.Empty;
        public string Sport { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string RinkSize { get; set; } = string.Empty;
    }
} 