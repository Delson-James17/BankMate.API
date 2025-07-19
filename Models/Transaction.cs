using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankMate.API.Models
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Account")]
        public Guid AccountId { get; set; }

        [Required]
        public string Type { get; set; } = "Deposit";

        [Required]
        public decimal Amount { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; }

        public Account? Account { get; set; }
    }

}
