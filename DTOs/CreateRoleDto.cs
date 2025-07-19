namespace BankMate.API.DTOs
{
    public class CreateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; 
    }
    public class AssignRoleDto
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }

    }
}
