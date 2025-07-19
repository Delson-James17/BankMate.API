using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Principal;

namespace BankMate.API.Models
{
    public class Users
    {
        [Key]
        public Guid Id { get; set; }
        [Required(ErrorMessage = "FirstName is required")]
        [StringLength(50, ErrorMessage = "FirstName cannot be longer than 50 characters.")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "LastName is required")]
        [StringLength(50, ErrorMessage = "LastName cannot be longer than 50 characters.")]
        public string LastName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;
        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required")]
        public string PasswordHash { get; set; } = string.Empty;
        [Required(ErrorMessage = "Roles is required")]
        [Range(0, 2, ErrorMessage = "Role must be between 0 and 2. 0: User, 1: Admin, 2: SuperAdmin")]
        [ForeignKey("Roles")]
        public Guid RoleId { get; set; }
        public Roles? Role { get; set; } = null;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Account? Account { get; set; }
        public bool IsLocked { get; set; } = false;
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    }
}
