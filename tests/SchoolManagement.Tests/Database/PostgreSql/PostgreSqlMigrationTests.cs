using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Persistence;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Database.PostgreSql;

[Collection(PostgreSqlTestCollection.Name)]
[Trait("Category", "PostgreSQL")]
public sealed class PostgreSqlMigrationTests
{
    private readonly PostgreSqlSchoolManagementApiFactory _factory;

    public PostgreSqlMigrationTests(PostgreSqlSchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Migrations_apply_to_empty_postgresql_database_and_create_key_tables()
    {
        using var scope = _factory.CreateDbContextScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        Assert.Empty(pendingMigrations);
        Assert.True(await TableExistsAsync(dbContext, "Users"));
        Assert.True(await TableExistsAsync(dbContext, "Students"));
        Assert.True(await TableExistsAsync(dbContext, "Fees"));
        Assert.True(await TableExistsAsync(dbContext, "Payments"));
        Assert.True(await TableExistsAsync(dbContext, "Results"));
    }

    private static async Task<bool> TableExistsAsync(AppDbContext dbContext, string tableName)
    {
        var connection = dbContext.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public'
                      AND table_name = @tableName
                );
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            return command.ExecuteScalar() is true;
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }
}
