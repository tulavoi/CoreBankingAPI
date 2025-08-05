using CoreBanking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Threading;

namespace CoreBanking.MigrationService
{
	public class Worker(IHostApplicationLifetime lifeTime, IServiceProvider serviceProvider, ILogger<Worker> logger) : BackgroundService
	{
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			try
			{
				logger.LogInformation("Migrating the database...");

				using var scope = serviceProvider.CreateScope();
				var dbContext = scope.ServiceProvider.GetRequiredService<CoreBankingDbContext>();

				logger.LogInformation("Ensuring database exists and is up to date...");
				await EnsureDatabaseAsync(dbContext, stoppingToken);

				logger.LogInformation("Running migration...");
				await RunMigrationAsync(dbContext, stoppingToken);
				//await SeedDataAsync(dbContext, cancellationToken);

				logger.LogInformation("Database migration completed successfully.");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred while executing the migration service.");
				throw;
			}

			lifeTime.StopApplication();
		}

		private async Task EnsureDatabaseAsync(CoreBankingDbContext dbContext, CancellationToken cancellationToken)
		{
			logger.LogInformation("Ensuring database exists...");

			var dbCreator = dbContext.GetService<IRelationalDatabaseCreator>();

			var strategy = dbContext.Database.CreateExecutionStrategy();
			await strategy.ExecuteAsync(async () =>
			{
				if (!await dbCreator.ExistsAsync(cancellationToken))
				{
					await dbCreator.CreateAsync(cancellationToken);
				}
			});
		}

		private static async Task RunMigrationAsync(CoreBankingDbContext dbContext, CancellationToken cancellationToken)
		{
			var strategy = dbContext.Database.CreateExecutionStrategy();
			await strategy.ExecuteAsync(async () =>
			{
				// Run migration in a transaction to avoid partial migration if it fails.
				await dbContext.Database.MigrateAsync(cancellationToken);
			});
		}

		private static async Task SeedDataAsync(CoreBankingDbContext dbContext, CancellationToken cancellationToken)
		{
			var strategy = dbContext.Database.CreateExecutionStrategy();
			await strategy.ExecuteAsync(async () =>
			{
				// Seed the database
				await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
				await dbContext.SaveChangesAsync(cancellationToken);
				await transaction.CommitAsync(cancellationToken);
			});
		}
	}
}
