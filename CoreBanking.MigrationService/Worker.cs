namespace CoreBanking.MigrationService
{
	public class Worker(IHostApplicationLifetime lifeTime, ILogger<Worker> logger) : BackgroundService
	{
		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			lifeTime.StopApplication();
			return Task.CompletedTask;
		}
	}
}
