using BankMate.API.Data;
using BankMate.API.Models;

namespace BankMate.API.Helpers
{
    public static class ActivityLogger
    {
        public static async Task LogAsync(AppDbContext context, Guid? userId, string action, string description, string name)
        {
            var log = new ActivityLogs
            {
                UserId = userId,
                Action = action,
                Description = description,
                Name = name
            };
            context.ActivityLogs.Add(log);
            await context.SaveChangesAsync();
        }
    }
}

