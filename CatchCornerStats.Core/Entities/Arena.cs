using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CatchCornerStats.Core.Entities
{
    [Table("VW_Arena", Schema = "powerBI")]
    public class Arena
    {
        [Column("FacilityId")]
        public int? FacilityId { get; set; }

        [Column("Facility")]
        public string? Facility { get; set; }

        [Column("NeighborhoodId")]
        public int? NeighborhoodId { get; set; }

        [Column("IsAvailable")]
        public bool? IsAvailable { get; set; }

        [Column("GoLiveDate")]
        public DateTime? GoLiveDate { get; set; }

        [Column("ListingRestrictionDays")]
        public int? ListingRestrictionDays { get; set; }

        //[Column("ListingRestrictionHours")]
        //public float? ListingRestrictionHours { get; set; }

        //[Column("ListingRestrictionTriggerPoint")]
        //public int? ListingRestrictionTriggerPoint { get; set; }

        [Column("Area")]
        public string? Area { get; set; }

        //[Column("Latitude")]
        //public double? Latitude { get; set; }

        //[Column("Longitude")]
        //public double? Longitude { get; set; }

        [Column("RinkId")]
        public int? RinkId { get; set; }

        [Column("Arena Rink")]
        public string? ArenaRink { get; set; }

        [Column("Size")]
        public string? Size { get; set; }

        [Column("Sport")]
        public string? Sport { get; set; }

        [Column("Parent")]
        public string? Parent { get; set; }

        [Column("Hidden")]
        public bool? Hidden { get; set; }

        [Column("Insurance Type")]
        public string? InsuranceType { get; set; }

        [Column("Csv Type")]
        public string? CsvType { get; set; }

        [Column("External System")]
        public string? ExternalSystem { get; set; }
    }
}
