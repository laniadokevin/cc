using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatchCornerStats.Core.Entities
{
    [Table("VW_Bookings", Schema = "powerBI")]
    public class Booking
    {
        [Column("Booking Number")]
        public int BookingNumber { get; set; }

        [Column("Facility")]
        public string Facility { get; set; }

        [Column("FacilityId")]
        public int FacilityId { get; set; }

        [Column("NeighborhoodId")]
        public int NeighborhoodId { get; set; }

        [Column("RinkId")]
        public int RinkId { get; set; }

        [Column("Arena Rink")]
        public string ArenaRink { get; set; }

        [Column("Sport")]
        public string Sport { get; set; }

        [Column("Created Date UTC")]
        public DateTime CreatedDateUtc { get; set; }

        [Column("Happening Date")]
        public DateTime HappeningDate { get; set; }

        [Column("StartTime")]
        public TimeSpan StartTime { get; set; }

        [Column("EndTime")]
        public TimeSpan EndTime { get; set; }

        [Column("Charged Amount")]
        public decimal? ChargedAmount { get; set; }

        [Column("Insurance")]
        public string Insurance { get; set; }

        [Column("Purchased By")]
        public string PurchasedBy { get; set; }

        [Column("Status")]
        public string Status { get; set; }

        [NotMapped]
        public string RinkSize { get; set; }

        [NotMapped]
        public string City { get; set; }

    }
}
