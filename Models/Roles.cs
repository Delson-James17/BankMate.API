using System.ComponentModel.DataAnnotations;

namespace BankMate.API.Models
{
    public class Roles
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ICollection<Users> Users { get; set; } = new List<Users>();
    }
}
