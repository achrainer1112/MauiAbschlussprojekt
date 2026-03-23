using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    [Table("water_entries")]
    public class WaterEntry
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("amount_ml")]
        public int AmountMl { get; set; }

        [Required]
        [Column("logged_at")]
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
