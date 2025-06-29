using CatchCornerStats.Core.Results;
using CatchCornerStats.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CatchCornerStats.Data.Repositories
{
    public class StatsRepository : IStatsRepository
    {
        private readonly AppDbContext _context;

        public StatsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<double> GetAverageLeadTimeAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities)
        {
            var query = from b in _context.Bookings
                        join a in _context.Arenas on b.FacilityId equals a.FacilityId
                        select new
                        {
                            b.CreatedDateUtc,
                            b.HappeningDate,
                            a.Sport,
                            a.Area,
                            a.Size,
                            b.Facility
                        };

            if (sports != null && sports.Any())
                query = query.Where(x => sports.Contains(x.Sport));

            if (cities != null && cities.Any())
                query = query.Where(x => cities.Contains(x.Area));

            if (rinkSizes != null && rinkSizes.Any())
                query = query.Where(x => rinkSizes.Contains(x.Size));

            if (facilities != null && facilities.Any())
                query = query.Where(x => facilities.Contains(x.Facility));

            var leadTimeDays = await query
                .Where(x => x.CreatedDateUtc != null && x.HappeningDate != null)
                .Select(x => EF.Functions.DateDiffDay(x.CreatedDateUtc, x.HappeningDate))
                .ToListAsync();

            if (leadTimeDays.Count == 0) return 0;

            return leadTimeDays.Average();
        }
        public async Task<Dictionary<string, double>> GetLeadTimeBreakdownAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
            var query = from b in _context.Bookings
                        join a in _context.Arenas
                        on b.FacilityId equals a.FacilityId
                        select new
                        {
                            b.CreatedDateUtc,
                            b.HappeningDate,
                            a.Sport,
                            a.Area,
                            a.Size,
                            b.Facility
                        };

            if (sports != null && sports.Any())
                query = query.Where(x => sports.Contains(x.Sport));

            if (cities != null && cities.Any())
                query = query.Where(x => cities.Contains(x.Area));

            if (rinkSizes != null && rinkSizes.Any())
                query = query.Where(x => rinkSizes.Contains(x.Size));

            if (facilities != null && facilities.Any())
                query = query.Where(x => facilities.Contains(x.Facility));

            // Filtros de fecha
            if (createdDateFrom.HasValue)
                query = query.Where(x => x.CreatedDateUtc >= createdDateFrom.Value);
            if (createdDateTo.HasValue)
                query = query.Where(x => x.CreatedDateUtc <= createdDateTo.Value);
            if (happeningDateFrom.HasValue)
                query = query.Where(x => x.HappeningDate >= happeningDateFrom.Value);
            if (happeningDateTo.HasValue)
                query = query.Where(x => x.HappeningDate <= happeningDateTo.Value);

            var leadTimeDays = await query
                .Where(x => x.CreatedDateUtc != null && x.HappeningDate != null)
                .Select(x => EF.Functions.DateDiffDay(x.CreatedDateUtc, x.HappeningDate))
                .ToListAsync();

            if (leadTimeDays.Count == 0) return new Dictionary<string, double>();

            var breakdown = leadTimeDays
                .Where(d => d >= 0)
                .GroupBy(d => d <= 30 ? d.ToString() : "+30")
                .Select(g => new
                {
                    DaysInAdvance = g.Key,
                    Percentage = (double)g.Count() / leadTimeDays.Count * 100
                })
                .OrderBy(x => x.DaysInAdvance == "+30" ? 999 : int.Parse(x.DaysInAdvance))
                .ToDictionary(x => x.DaysInAdvance, x => x.Percentage);

            return breakdown;
        }
        public async Task<Dictionary<string, double>> GetBookingsByDayAsync(string? sport, string? city, string? rinkSize, string? facility, int? month, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
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

            if (month.HasValue)
                query = query.Where(x => x.HappeningDate.Month == month);

            // Filtros de fecha
            if (createdDateFrom.HasValue)
                query = query.Where(x => x.CreatedDateUtc >= createdDateFrom.Value);
            if (createdDateTo.HasValue)
                query = query.Where(x => x.CreatedDateUtc <= createdDateTo.Value);
            if (happeningDateFrom.HasValue)
                query = query.Where(x => x.HappeningDate >= happeningDateFrom.Value);
            if (happeningDateTo.HasValue)
                query = query.Where(x => x.HappeningDate <= happeningDateTo.Value);

            // Total bookings únicos
            var totalBookings = await query.Select(x => x.BookingNumber).Distinct().CountAsync();

            if (totalBookings == 0) return new Dictionary<string, double>();

            var result = query
                 .AsEnumerable()
                 .GroupBy(x => x.HappeningDate.DayOfWeek)
                 .Select(g => new
                 {
                     DayOfWeek = g.Key.ToString(),
                     UniqueBookings = g.Select(x => x.BookingNumber).Distinct().Count(),
                     Percentage = (double)g.Select(x => x.BookingNumber).Distinct().Count() / totalBookings * 100
                 })
                 .ToDictionary(x => x.DayOfWeek, x => x.Percentage);

            return result;
        }

        public async Task<BookingsByStartTimeResult> GetBookingsByStartTimeAsync(string? sport, string? city, string? rinkSize, string? facility, int? month, int? dayOfWeek, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
            var query = from b in _context.Bookings
                        join a in _context.Arenas on b.FacilityId equals a.FacilityId
                        select new
                        {
                            b.HappeningDate,
                            b.StartTime,
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
            if (month.HasValue)
                query = query.Where(x => x.HappeningDate.Month == month);
            if (dayOfWeek.HasValue)
                query = query.Where(x => (int)x.HappeningDate.DayOfWeek == dayOfWeek.Value);
            if (createdDateFrom.HasValue)
                query = query.Where(x => x.CreatedDateUtc >= createdDateFrom.Value);
            if (createdDateTo.HasValue)
                query = query.Where(x => x.CreatedDateUtc <= createdDateTo.Value);
            if (happeningDateFrom.HasValue)
                query = query.Where(x => x.HappeningDate >= happeningDateFrom.Value);
            if (happeningDateTo.HasValue)
                query = query.Where(x => x.HappeningDate <= happeningDateTo.Value);

            var grouped = await query
                .GroupBy(x => x.StartTime.Hours)
                .Select(g => new {
                    StartHour = g.Key,
                    UniqueBookings = g.Select(x => x.BookingNumber).Distinct().Count()
                })
                .ToListAsync();

            var totalBookings = grouped.Sum(g => g.UniqueBookings);
            var data = grouped.ToDictionary(
                g => {
                    int hour = g.StartHour;
                    string ampm = hour < 12 ? "AM" : "PM";
                    int hour12 = hour % 12 == 0 ? 12 : hour % 12;
                    return $"{hour12}:00 {ampm}";
                },
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
            var query = from b in _context.Bookings
                        join a in _context.Arenas on b.FacilityId equals a.FacilityId
                        select new
                        {
                            StartTime = b.StartTime,
                            EndTime = b.EndTime,
                            Sport = a.Sport,
                            City = a.Area,
                            RinkSize = a.Size,
                            Facility = b.Facility,
                            BookingNumber = b.BookingNumber,
                            CreatedDateUtc = b.CreatedDateUtc,
                            HappeningDate = b.HappeningDate
                        };

            if (!string.IsNullOrEmpty(sport))
                query = query.Where(x => x.Sport == sport);

            if (!string.IsNullOrEmpty(city))
                query = query.Where(x => x.City == city);

            if (!string.IsNullOrEmpty(rinkSize))
                query = query.Where(x => x.RinkSize == rinkSize);

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

            // Total bookings únicos
            var totalBookings = await query.Select(x => x.BookingNumber).Distinct().CountAsync();

            if (totalBookings == 0) return new Dictionary<string, double>();

            var durations = query
                .AsEnumerable()
                .GroupBy(x => Math.Round((x.EndTime - x.StartTime).TotalHours, 1))
                .Select(g => new
                {
                    Duration = $"{g.Key} hr",
                    UniqueBookings = g.Select(x => x.BookingNumber).Distinct().Count(),
                    Percentage = (double)g.Select(x => x.BookingNumber).Distinct().Count() / totalBookings * 100
                })
                .ToDictionary(x => x.Duration, x => x.Percentage);

            return durations;
        }
        public async Task<List<MonthlyReportResult>> GetMonthlyReportAsync(string? sport, string? city, string? rinkSize, string? facility)
        {
            var query = from b in _context.Bookings
                        join a in _context.Arenas on b.FacilityId equals a.FacilityId
                        select new
                        {
                            b.HappeningDate,
                            b.Facility,
                            a.Sport,
                            a.Area,
                            a.Size,
                            b.BookingNumber
                        };

            if (!string.IsNullOrEmpty(sport))
                query = query.Where(x => x.Sport == sport);

            if (!string.IsNullOrEmpty(city))
                query = query.Where(x => x.Area == city);

            if (!string.IsNullOrEmpty(rinkSize))
                query = query.Where(x => x.Size == rinkSize);

            if (!string.IsNullOrEmpty(facility))
                query = query.Where(x => x.Facility == facility);

            var bookings = await query
                .GroupBy(x => new
                {
                    x.Facility,
                    Month = x.HappeningDate.Month,
                    Year = x.HappeningDate.Year
                })
                .Select(g => new
                {
                    FacilityName = g.Key.Facility,
                    MonthYear = $"{g.Key.Month}/{g.Key.Year}",
                    TotalBookings = g.Select(x => x.BookingNumber).Distinct().Count()
                })
                .ToListAsync();

            var report = new List<MonthlyReportResult>();

            foreach (var booking in bookings)
            {
                var previousMonthBookings = bookings
                    .FirstOrDefault(x =>
                        x.FacilityName == booking.FacilityName &&
                        x.MonthYear == $"{(booking.MonthYear.Split('/')[0] == "1" ? 12 : int.Parse(booking.MonthYear.Split('/')[0]) - 1)}/{(booking.MonthYear.Split('/')[0] == "1" ? int.Parse(booking.MonthYear.Split('/')[1]) - 1 : int.Parse(booking.MonthYear.Split('/')[1]))}")
                    ?.TotalBookings;

                var percentageDrop = previousMonthBookings.HasValue && previousMonthBookings.Value > 0
                    ? (double)(previousMonthBookings.Value - booking.TotalBookings) / previousMonthBookings.Value * 100
                    : (double?)null;

                var isFlagged = booking.TotalBookings >= 10 &&
                                previousMonthBookings.HasValue &&
                                percentageDrop.HasValue &&
                                percentageDrop.Value >= 50;

                report.Add(new MonthlyReportResult
                {
                    FacilityName = booking.FacilityName,
                    MonthYear = booking.MonthYear,
                    TotalBookings = booking.TotalBookings,
                    PreviousMonthBookings = previousMonthBookings,
                    PercentageDrop = percentageDrop,
                    IsFlagged = isFlagged
                });
            }

            return report;
        }
        public async Task<List<SportComparisonResult>> GetSportComparisonReportAsync(string? city, int? month)
        {
            var query = from b in _context.Bookings
                        join a in _context.Arenas on b.FacilityId equals a.FacilityId
                        select new
                        {
                            Sport = a.Sport,
                            City = a.Area,
                            b.HappeningDate,
                            b.BookingNumber
                        };

            // Filtrar nulos/vacíos
            query = query.Where(x => !string.IsNullOrEmpty(x.Sport) && !string.IsNullOrEmpty(x.City));

            if (!string.IsNullOrEmpty(city))
                query = query.Where(x => x.City == city);

            if (month.HasValue)
                query = query.Where(x => x.HappeningDate.Month == month);

            // Agrupar por Sport y City, pero contar bookings únicos
            var sportBookings = await query
                .GroupBy(x => new { x.Sport, x.City })
                .Select(g => new
                {
                    Sport = g.Key.Sport,
                    City = g.Key.City,
                    TotalBookings = g.Select(x => x.BookingNumber).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalBookings)
                .ToListAsync();

            var report = new List<SportComparisonResult>();

            var top6 = sportBookings.Take(6).ToList();
            var top8 = sportBookings.Take(8).ToList();
            var mostPopularSportBookings = sportBookings.FirstOrDefault()?.TotalBookings ?? 0;

            foreach (var booking in sportBookings)
            {
                var isFlaggedTop6 = !top6.Any(s => s.Sport == booking.Sport) &&
                                    top6.Any(s => s.TotalBookings < booking.TotalBookings);

                var isFlaggedTop8 = !top8.Any(s => s.Sport == booking.Sport) &&
                                    top8.Any(s => s.TotalBookings < booking.TotalBookings);

                var isFlaggedHighBookings = booking.TotalBookings >= 60 ||
                                            (mostPopularSportBookings > 0 &&
                                             booking.TotalBookings >= mostPopularSportBookings * 0.05);

                report.Add(new SportComparisonResult
                {
                    Sport = booking.Sport,
                    City = booking.City,
                    TotalBookings = booking.TotalBookings,
                    IsFlaggedTop6 = isFlaggedTop6,
                    IsFlaggedTop8 = isFlaggedTop8,
                    IsFlaggedHighBookings = isFlaggedHighBookings
                });
            }

            return report;
        }
        public async Task<List<string>> GetSportsAsync()
        {
            return await _context.Arenas
                .Select(x => x.Sport)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }
        public async Task<List<string>> GetCitiesAsync()
        {
            return await _context.Arenas
                .Select(x => x.Area)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }
        public async Task<List<string>> GetRinkSizesAsync()
        {
            return await _context.Arenas
                .Select(x => x.Size)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }
        public async Task<List<string>> GetFacilitiesAsync()
        {
            return await _context.Arenas
                .Select(x => x.Facility)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }
        public async Task<List<StatsRawDto>> GetAllStatsRawAsync()
        {
            return await (
                from b in _context.Bookings
                join a in _context.Arenas on b.FacilityId equals a.FacilityId
                select new StatsRawDto
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
                }).ToListAsync();
        }
        public async Task<(double averageLeadTime, int totalBookings)> GetAverageLeadTimeWithCountAsync(List<string>? sports, List<string>? cities, List<string>? rinkSizes, List<string>? facilities, DateTime? createdDateFrom, DateTime? createdDateTo, DateTime? happeningDateFrom, DateTime? happeningDateTo)
        {
            var query = from b in _context.Bookings
                        join a in _context.Arenas on b.FacilityId equals a.FacilityId
                        select new
                        {
                            b.BookingNumber,
                            b.CreatedDateUtc,
                            b.HappeningDate,
                            a.Sport,
                            a.Area,
                            a.Size,
                            b.Facility
                        };

            if (sports != null && sports.Any(x => !string.IsNullOrWhiteSpace(x)))
                query = query.Where(x => sports.Contains(x.Sport));

            if (cities != null && cities.Any(x => !string.IsNullOrWhiteSpace(x)))
                query = query.Where(x => cities.Contains(x.Area));

            if (rinkSizes != null && rinkSizes.Any(x => !string.IsNullOrWhiteSpace(x)))
                query = query.Where(x => rinkSizes.Contains(x.Size));

            if (facilities != null && facilities.Any(x => !string.IsNullOrWhiteSpace(x)))
                query = query.Where(x => facilities.Contains(x.Facility));

            // Filtros de fecha
            if (createdDateFrom.HasValue)
                query = query.Where(x => x.CreatedDateUtc >= createdDateFrom.Value);
            if (createdDateTo.HasValue)
                query = query.Where(x => x.CreatedDateUtc <= createdDateTo.Value);
            if (happeningDateFrom.HasValue)
                query = query.Where(x => x.HappeningDate >= happeningDateFrom.Value);
            if (happeningDateTo.HasValue)
                query = query.Where(x => x.HappeningDate <= happeningDateTo.Value);

            var leadTimeQuery = query
                .Where(x => x.CreatedDateUtc != null && x.HappeningDate != null)
                .Select(x => new { x.BookingNumber, x.CreatedDateUtc, x.HappeningDate });

            var leadTimeDays = await leadTimeQuery
                .Select(x => EF.Functions.DateDiffDay(x.CreatedDateUtc, x.HappeningDate))
                .ToListAsync();

            var totalBookings = await leadTimeQuery
                .Select(x => x.BookingNumber)
                .Distinct()
                .CountAsync();

            if (leadTimeDays.Count == 0) return (0, 0);

            return (leadTimeDays.Average(), totalBookings);
        }
    }
}