using BankMate.API.Data;
using Microsoft.EntityFrameworkCore;

namespace BankMate.API.Services
{
    public class AccountMonitorService(IServiceProvider serviceProvider, ILogger<AccountMonitorService> logger) : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<AccountMonitorService> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AccountMonitorService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var now = DateTime.UtcNow;
                    var sixMonthsAgo = now.AddMonths(-6);
                    var oneWeekAgo = now.AddDays(-7);

                    var usersToLock = await dbContext.Users
                        .Include(u => u.Account)
                        .Where(u => !u.IsLocked &&
                            (
                                u.LastActivity <= sixMonthsAgo ||
                                (u.Account != null && u.Account.Balance == 0 && u.Account.LastUpdated <= oneWeekAgo)
                            )
                        ).ToListAsync(stoppingToken);

                    foreach (var user in usersToLock)
                    {
                        user.IsLocked = true;
                        _logger.LogInformation($"User {user.Email} locked due to inactivity or zero balance.");
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while running AccountMonitorService.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("AccountMonitorService stopped.");
        }
    }
}
