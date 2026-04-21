using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Persistence;
using SchoolManagement.Persistence.Seed;
using SchoolManagement.API.Common;
using Microsoft.Extensions.Logging;

namespace SchoolManagement.API.Extensions;

public static class ApplicationBuilderExtensions
{
    private const string PostgresProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private const long DatabaseInitializationLockKey = 741_852_963L;
    private static readonly TimeSpan DefaultAdvisoryLockRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DefaultAdvisoryLockTimeout = TimeSpan.FromMinutes(1);

    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        var autoMigrate = app.Configuration.GetValue("Database:AutoMigrate", app.Environment.IsDevelopment());
        var seedDemoData = app.Configuration.GetValue("Database:SeedDemoData", app.Environment.IsDevelopment());
        var advisoryLockRetryDelay = GetConfiguredDuration(
            app.Configuration,
            "Database:InitializationLockRetryDelaySeconds",
            DefaultAdvisoryLockRetryDelay);
        var advisoryLockTimeout = GetConfiguredDuration(
            app.Configuration,
            "Database:InitializationLockTimeoutSeconds",
            DefaultAdvisoryLockTimeout);
        var cancellationToken = app.Lifetime.ApplicationStopping;
        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Startup.DatabaseInitialization");

        if (advisoryLockTimeout < advisoryLockRetryDelay)
        {
            throw new InvalidOperationException(
                "Database:InitializationLockTimeoutSeconds must be greater than or equal to Database:InitializationLockRetryDelaySeconds.");
        }

        logger.LogInformation(
            "Database initialization starting. AutoMigrate={AutoMigrate}, SeedDemoData={SeedDemoData}, LockRetryDelaySeconds={LockRetryDelaySeconds}, LockTimeoutSeconds={LockTimeoutSeconds}",
            autoMigrate,
            seedDemoData,
            advisoryLockRetryDelay.TotalSeconds,
            advisoryLockTimeout.TotalSeconds);

        if (!autoMigrate && !seedDemoData)
        {
            logger.LogInformation("Database initialization skipped because both AutoMigrate and SeedDemoData are disabled.");
            var initializationState = app.Services.GetRequiredService<IAuditInitializationState>();
            initializationState.IsReady = true;
            return;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var usesPostgres = string.Equals(context.Database.ProviderName, PostgresProviderName, StringComparison.Ordinal);

        logger.LogInformation("Resolved database provider: {ProviderName}", context.Database.ProviderName);

        if (usesPostgres)
        {
            logger.LogInformation("Opening database connection for initialization lock.");
            await context.Database.OpenConnectionAsync(cancellationToken);
            logger.LogInformation(
                "Database connection opened. Attempting advisory lock with key {LockKey}.",
                DatabaseInitializationLockKey);

            await AcquireInitializationLockAsync(
                context,
                logger,
                advisoryLockRetryDelay,
                advisoryLockTimeout,
                cancellationToken);
            logger.LogInformation("Advisory lock acquired.");
        }

        try
        {
            if (autoMigrate)
            {
                logger.LogInformation("Starting database migration step.");
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database migration step completed.");
            }

            if (seedDemoData)
            {
                logger.LogInformation("Starting demo data seed step.");
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                await DataSeeder.SeedAsync(context, passwordHasher, cancellationToken);
                logger.LogInformation("Demo data seed step completed.");
            }
        }
        finally
        {
            if (usesPostgres && context.Database.GetDbConnection().State == System.Data.ConnectionState.Open)
            {
                try
                {
                    logger.LogInformation("Releasing advisory lock.");
                    await context.Database.ExecuteSqlRawAsync(
                        $"SELECT pg_advisory_unlock({DatabaseInitializationLockKey})",
                        CancellationToken.None);
                    logger.LogInformation("Advisory lock released.");
                }
                finally
                {
                    logger.LogInformation("Closing database connection used for initialization.");
                    await context.Database.CloseConnectionAsync();
                    // NOTE: Advisory locks are session-scoped and auto-released on disconnect,
                    // so even if ApplicationStopping fires, the lock will be properly released.
                }
            }
        }

        logger.LogInformation("Database initialization completed successfully.");
        var auditInitializationState = app.Services.GetRequiredService<IAuditInitializationState>();
        auditInitializationState.IsReady = true;
    }

    private static async Task AcquireInitializationLockAsync(
        AppDbContext context,
        ILogger logger,
        TimeSpan retryDelay,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var waitTimer = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use raw connection to avoid EF Core's SqlQueryRaw mapping issues with scalar results
            var connection = context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;

            try
            {
                if (!wasOpen)
                {
                    await connection.OpenAsync(cancellationToken);
                }

                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT pg_try_advisory_lock({DatabaseInitializationLockKey})";
                command.CommandTimeout = 10;  // Prevent individual lock queries from hanging (normally <1s)
                var result = await command.ExecuteScalarAsync(cancellationToken);
                var acquired = result is true;

                if (acquired)
                {
                    return;
                }
            }
            finally
            {
                if (!wasOpen && connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }

            var elapsed = waitTimer.Elapsed;
            if (elapsed >= timeout)
            {
                throw new TimeoutException(
                    $"Timed out after {timeout.TotalSeconds:F0} seconds waiting for database initialization advisory lock {DatabaseInitializationLockKey}.");
            }

            logger.LogWarning(
                "Database initialization lock {LockKey} is currently held by another session. Waiting {RetryDelaySeconds} seconds before retry. Elapsed={ElapsedSeconds:F0}s",
                DatabaseInitializationLockKey,
                retryDelay.TotalSeconds,
                elapsed.TotalSeconds);

            await Task.Delay(retryDelay, cancellationToken);
        }
    }

    private static TimeSpan GetConfiguredDuration(
        IConfiguration configuration,
        string key,
        TimeSpan defaultValue)
    {
        var configuredSeconds = configuration.GetValue<double?>(key);
        if (configuredSeconds is null)
        {
            return defaultValue;
        }

        if (configuredSeconds < 1)
        {
            throw new InvalidOperationException($"{key} must be at least 1 second.");
        }

        if (configuredSeconds > 600)
        {
            throw new InvalidOperationException($"{key} must not exceed 600 seconds (10 minutes).");
        }

        return TimeSpan.FromSeconds(configuredSeconds.Value);
    }
}
