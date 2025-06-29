using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatchCornerStats.Core.Entities
{
    [Table("VW_Listings", Schema = "powerBI")]
    public class Listing
    {
        [Column("FacilityId")]
        public int FacilityId { get; set; }

        [Column("NeighborhoodId")]
        public int NeighborhoodId { get; set; }

        [Column("RinkId")]
        public int RinkId { get; set; }

        [Column("Facility")]
        public string Facility { get; set; }

        [Column("ArenaRink")]
        public string ArenaRink { get; set; }

        [Column("Area")]
        public string Area { get; set; }

        [Column("Sport")]
        public string Sport { get; set; }

        [Column("Created Date UTC")]
        public DateTime CreatedDateUtc { get; set; }

        [Column("Update Date UTC")]
        public DateTime? UpdateDateUtc { get; set; }

        [Column("Happening Date")]
        public DateTime HappeningDate { get; set; }

        [Column("StartTime")]
        public TimeSpan StartTime { get; set; }

        [Column("EndTime")]
        public TimeSpan EndTime { get; set; }

        [Column("Status")]
        public string Status { get; set; }

        [Column("Deactivation Reason")]
        public string DeactivationReason { get; set; }

        [Column("PricePerHourWithoutTax")]
        public decimal? PricePerHourWithoutTax { get; set; }

        [Column("PricePerHourWithTax")]
        public decimal? PricePerHourWithTax { get; set; }

        [Column("Category")]
        public string Category { get; set; }
    }
}
