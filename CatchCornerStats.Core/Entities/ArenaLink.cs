using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatchCornerStats.Core.Entities
{
    [Table("VW_ArenaLink", Schema = "powerBI")]
    public class ArenaLink
    {
        [Column("Facility")]
        public string Facility { get; set; }

        [Column("DirectLink")]
        public string DirectLink { get; set; }

        [Column("EmbeddedLink")]
        public string EmbeddedLink { get; set; }
    }
}
