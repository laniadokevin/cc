using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatchCornerStats.Core.Entities
{
    [Table("VW_Organization", Schema = "powerBI")]
    public class Organization
    {
        [Key]
        [Column("Organization")]
        public string OrganizationName { get; set; }

        [Column("Arena")]
        public string Arena { get; set; }
    }
}
