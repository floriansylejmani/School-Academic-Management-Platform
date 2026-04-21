using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Persistence;
using SchoolManagement.Persistence.Seed;

namespace SchoolManagement.Tests.Infrastructure;

public sealed class DatabaseInitializationTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public DatabaseInitializationTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Migrations_are_registered_and_model_snapshot_is_in_sync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var migrations = dbContext.Database.GetMigrations().ToArray();

        Assert.Equal(
            [
                "20260410133459_InitialCreate",
                "20260411160000_AddResetTokens",
                "20260414120000_AddNotificationStudentId",
                "20260415183000_AddSubmissionsAndAiReviews",
                "20260415200000_AddFileUploads",
                "20260416165039_AddAuditLogTable"
            ],
            migrations);
    }

    [Fact]
    public async Task SeedAsync_is_idempotent_on_retry()
    {
        await _factory.ResetDatabaseAsync();
        var baseline = await LoadCountsAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<SchoolManagement.Application.Common.Interfaces.IPasswordHasher>();
            await DataSeeder.SeedAsync(dbContext, passwordHasher, CancellationToken.None);
        }

        var afterRetry = await LoadCountsAsync();

        Assert.Equal(baseline, afterRetry);
        Assert.Equal(0, afterRetry.DuplicatePaymentReferences);
    }

    [Fact]
    public async Task SeedAsync_completes_when_partial_demo_data_already_exists()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Subjects.Add(new Subject
            {
                Name = "Mathematics",
                Code = "MATH",
                Description = "Advanced mathematics including calculus and algebra"
            });

            await dbContext.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<SchoolManagement.Application.Common.Interfaces.IPasswordHasher>();
            await DataSeeder.SeedAsync(dbContext, passwordHasher, CancellationToken.None);
        }

        var counts = await LoadCountsAsync();

        Assert.Equal(4, counts.Roles);
        Assert.Equal(37, counts.Users);
        Assert.Equal(8, counts.Subjects);
        Assert.Equal(3, counts.Classes);
        Assert.Equal(4, counts.Teachers);
        Assert.Equal(8, counts.Parents);
        Assert.Equal(24, counts.Students);
        Assert.Equal(24, counts.Enrollments);
        Assert.Equal(14, counts.Assignments);
        Assert.Equal(20, counts.Exams);
        Assert.True(counts.Payments > 0);
        Assert.True(counts.Notifications > 0);
        Assert.Equal(0, counts.DuplicatePaymentReferences);
    }

    private async Task<SeedCounts> LoadCountsAsync()
    {
        return await _factory.ExecuteDbContextAsync(async dbContext =>
        {
            var duplicatePaymentReferences = await dbContext.Payments
                .Where(x => x.TransactionReference != null)
                .GroupBy(x => x.TransactionReference)
                .Where(x => x.Count() > 1)
                .CountAsync();

            return new SeedCounts(
                await dbContext.Roles.CountAsync(),
                await dbContext.Users.CountAsync(),
                await dbContext.Subjects.CountAsync(),
                await dbContext.AcademicClasses.CountAsync(),
                await dbContext.Teachers.CountAsync(),
                await dbContext.Parents.CountAsync(),
                await dbContext.Students.CountAsync(),
                await dbContext.Enrollments.CountAsync(),
                await dbContext.TeacherSubjectAssignments.CountAsync(),
                await dbContext.TimetableEntries.CountAsync(),
                await dbContext.AttendanceRecords.CountAsync(),
                await dbContext.Exams.CountAsync(),
                await dbContext.Results.CountAsync(),
                await dbContext.Fees.CountAsync(),
                await dbContext.Payments.CountAsync(),
                await dbContext.Notifications.CountAsync(),
                duplicatePaymentReferences);
        });
    }

    private sealed record SeedCounts(
        int Roles,
        int Users,
        int Subjects,
        int Classes,
        int Teachers,
        int Parents,
        int Students,
        int Enrollments,
        int Assignments,
        int TimetableEntries,
        int AttendanceRecords,
        int Exams,
        int Results,
        int Fees,
        int Payments,
        int Notifications,
        int DuplicatePaymentReferences);
}
