using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Persistence.Services;

public sealed class AuditLogCleanupService : BackgroundService
{
    private readonly ILogger<AuditLogCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AuditCleanupOptions _options;
    private readonly TimeSpan _cleanupInterval;

    public AuditLogCleanupService(
        ILogger<AuditLogCleanupService> logger,
        IServiceProvider serviceProvider,
        IOptions<AuditCleanupOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _cleanupInterval = TimeSpan.FromHours(_options.CleanupIntervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit Log Cleanup Service started with interval: {Interval}", _cleanupInterval);

        // Initial delay to allow application to start up
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during audit log cleanup");
            }

            // Wait for the next cleanup interval
            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        if (!_options.EnableCleanup)
        {
            _logger.LogDebug("Audit log cleanup is disabled");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);
            
            _logger.LogInformation("Starting audit log cleanup for records older than {CutoffDate}", cutoffDate);

            // Use a more efficient approach for large datasets
            var totalDeleted = 0;
            var batchSize = _options.BatchSize;
            var hasMore = true;

            while (hasMore && !cancellationToken.IsCancellationRequested)
            {
                var oldLogs = await context.AuditLogs
                    .Where(log => log.Timestamp < cutoffDate)
                    .OrderBy(log => log.Timestamp)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!oldLogs.Any())
                {
                    hasMore = false;
                    break;
                }

                context.AuditLogs.RemoveRange(oldLogs);
                await context.SaveChangesAsync(cancellationToken);

                totalDeleted += oldLogs.Count;
                _logger.LogDebug("Deleted batch of {Count} audit logs", oldLogs.Count);

                // Small delay between batches to prevent overwhelming the database
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }

            _logger.LogInformation("Audit log cleanup completed. Total records deleted: {TotalDeleted}", totalDeleted);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Audit log cleanup was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform audit log cleanup");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Audit Log Cleanup Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}

public sealed class AuditCleanupOptions
{
    public bool EnableCleanup { get; set; } = true;
    public int RetentionDays { get; set; } = 90;
    public int CleanupIntervalHours { get; set; } = 6;
    public int BatchSize { get; set; } = 1000;
}
