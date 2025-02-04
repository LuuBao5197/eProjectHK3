using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using eProject.Repository;

public class OtpCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public OtpCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var users = await userRepository.GetUsersAsync();

                foreach (var user in users)
                {
                    if (user.OTPExpired.HasValue && user.OTPExpired < DateTime.UtcNow)
                    {
                        user.OTP = null;
                        user.OTPExpired = null;
                        await userRepository.UpdateUser(user);
                    }
                }
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Kiểm tra mỗi 1 phút
        }
    }
}
