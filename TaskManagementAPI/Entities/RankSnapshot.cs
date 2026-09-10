using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementAPI.Entities
{
    [Table("rank_snapshots")]
    public class RankSnapshot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime SnapshotDate { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]  // PostgreSQL 专用，如果是 SQL Server 则改为 nvarchar(max)
        public string Rankings { get; set; } = string.Empty;
    }
}
