using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankMate.API.Models
{
    public class Account
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public decimal Balance { get; set; } = 0;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public Users? User { get; set; }

        public ICollection<Transaction>? Transactions { get; set; }
    }

}
