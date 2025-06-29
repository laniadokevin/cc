using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatchCornerStats.Core.Entities
{
    [Table("VW_Neighborhoods", Schema = "powerBI")]
    public class Neighborhood
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Column("Latitude")]
        public double? Latitude { get; set; }

        [Column("Longitude")]
        public double? Longitude { get; set; }
    }
}