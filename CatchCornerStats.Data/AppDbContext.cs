using CatchCornerStats.Core.Entities;
using CatchCornerStats.Core.Results;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CatchCornerStats.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Arena> Arenas { get; set; }
        public DbSet<ArenaLink> ArenaLinks { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<Neighborhood> Neighborhoods { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Arena>().ToTable("VW_Arena", "powerBI").HasNoKey();
            modelBuilder.Entity<ArenaLink>().ToTable("VW_ArenaLink", "powerBI").HasNoKey();
            modelBuilder.Entity<Booking>().ToTable("VW_Bookings", "powerBI").HasNoKey();
            modelBuilder.Entity<Listing>().ToTable("VW_Listings", "powerBI").HasNoKey();
            modelBuilder.Entity<Neighborhood>().ToTable("VW_Neighborhoods", "powerBI").HasNoKey();
            modelBuilder.Entity<Organization>().ToTable("VW_Organization", "powerBI").HasNoKey();
            modelBuilder.Entity<BookingsByDayDto>().HasNoKey();

            modelBuilder.Entity<Arena>().HasNoKey()
                    .Property(a => a.FacilityId)
                    .HasColumnName("FacilityId");
            modelBuilder.Entity<ArenaLink>().HasNoKey();
            modelBuilder.Entity<Booking>().HasNoKey();
            modelBuilder.Entity<Listing>().HasNoKey();
            modelBuilder.Entity<Neighborhood>().HasNoKey();
            modelBuilder.Entity<Organization>().HasNoKey();
            modelBuilder.Entity<Arena>()
                        .Property(a => a.IsAvailable)
                        .HasConversion(
                            v => (v.HasValue ? v.Value : false) ? "1" : "0",
                            v => v == "1"
                        );

            modelBuilder.Entity<Arena>()
                .Property(a => a.Hidden)
                .HasConversion(
                    v => (v.HasValue ? v.Value : false) ? "1" : "0",
                    v => v == "1"
                );
            modelBuilder.Entity<Booking>()
                .Property(b => b.ChargedAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Listing>()
                .Property(l => l.PricePerHourWithoutTax)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Listing>()
                .Property(l => l.PricePerHourWithTax)
                .HasPrecision(18, 2);

        }

    }

}