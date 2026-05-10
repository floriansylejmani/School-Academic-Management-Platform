using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Persistence;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Database.PostgreSql;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlSchemaIntegrityTests
{
    private readonly PostgreSqlSchoolManagementApiFactory _factory;

    public PostgreSqlSchemaIntegrityTests(PostgreSqlSchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Payments_idempotency_key_index_is_unique_and_partial()
    {
        var indexDefinition = await GetIndexDefinitionAsync("IX_Payments_IdempotencyKey");

        Assert.NotNull(indexDefinition);
        Assert.Contains("CREATE UNIQUE INDEX", indexDefinition, StringComparison.Ordinal);
        Assert.Contains("\"IdempotencyKey\"", indexDefinition, StringComparison.Ordinal);
        Assert.Contains("IS NOT NULL", indexDefinition, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Results_have_unique_exam_student_index()
    {
        var indexDefinition = await GetIndexDefinitionAsync("IX_Results_ExamId_StudentId");

        Assert.NotNull(indexDefinition);
        Assert.Contains("CREATE UNIQUE INDEX", indexDefinition, StringComparison.Ordinal);
        Assert.Contains("\"ExamId\"", indexDefinition, StringComparison.Ordinal);
        Assert.Contains("\"StudentId\"", indexDefinition, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Student_identity_indexes_are_unique()
    {
        var studentCodeIndex = await GetIndexDefinitionAsync("IX_Students_StudentCode");
        var userEmailIndex = await GetIndexDefinitionAsync("IX_Users_Email");

        Assert.NotNull(studentCodeIndex);
        Assert.Contains("CREATE UNIQUE INDEX", studentCodeIndex, StringComparison.Ordinal);
        Assert.Contains("\"StudentCode\"", studentCodeIndex, StringComparison.Ordinal);

        Assert.NotNull(userEmailIndex);
        Assert.Contains("CREATE UNIQUE INDEX", userEmailIndex, StringComparison.Ordinal);
        Assert.Contains("\"Email\"", userEmailIndex, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Payment_lookup_indexes_exist()
    {
        var feeIndex = await GetIndexDefinitionAsync("IX_Payments_FeeId");
        var idempotencyIndex = await GetIndexDefinitionAsync("IX_Payments_IdempotencyKey");

        Assert.NotNull(feeIndex);
        Assert.Contains("\"FeeId\"", feeIndex, StringComparison.Ordinal);

        Assert.NotNull(idempotencyIndex);
        Assert.Contains("\"IdempotencyKey\"", idempotencyIndex, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Attendance_lookup_composite_index_exists()
    {
        var indexDefinition = await GetIndexDefinitionAsync("IX_AttendanceRecords_StudentId_ClassId_Date");

        Assert.NotNull(indexDefinition);
        Assert.Contains("\"StudentId\"", indexDefinition, StringComparison.Ordinal);
        Assert.Contains("\"ClassId\"", indexDefinition, StringComparison.Ordinal);
        Assert.Contains("\"Date\"", indexDefinition, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Fee_lookup_indexes_exist()
    {
        var studentIndex = await GetIndexDefinitionAsync("IX_Fees_StudentId");
        var statusDueDateIndex = await GetIndexDefinitionAsync("IX_Fees_Status_DueDate");

        Assert.NotNull(studentIndex);
        Assert.Contains("\"StudentId\"", studentIndex, StringComparison.Ordinal);

        Assert.NotNull(statusDueDateIndex);
        Assert.Contains("\"Status\"", statusDueDateIndex, StringComparison.Ordinal);
        Assert.Contains("\"DueDate\"", statusDueDateIndex, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Duplicate_result_for_same_exam_and_student_is_rejected_by_database()
    {
        await _factory.ResetDatabaseAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        Guid examId;
        Guid studentId;

        using (var scope = _factory.CreateDbContextScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var student = await CreateStudentAsync(dbContext, $"PG-RESULT-{unique}", $"pg.result.{unique}@school.test");
            var academicClass = new AcademicClass
            {
                Name = "PG Integrity",
                Section = unique,
                AcademicYear = "2026"
            };
            var subject = new Subject
            {
                Name = "PostgreSQL Integrity",
                Code = $"PGI-{unique}"
            };
            var exam = new Exam
            {
                Class = academicClass,
                Subject = subject,
                Title = "Schema Integrity Exam",
                ExamDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TotalMarks = 100
            };

            dbContext.Results.Add(new Result
            {
                Exam = exam,
                Student = student,
                MarksObtained = 88,
                Grade = "A"
            });

            await dbContext.SaveChangesAsync();
            examId = exam.Id;
            studentId = student.Id;

            dbContext.Results.Add(new Result
            {
                ExamId = examId,
                StudentId = studentId,
                MarksObtained = 72,
                Grade = "B"
            });

            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        }

        using var verificationScope = _factory.CreateDbContextScope();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var resultCount = await verificationDbContext.Results
            .CountAsync(x => x.ExamId == examId && x.StudentId == studentId);

        Assert.Equal(1, resultCount);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Duplicate_student_code_is_rejected_by_database()
    {
        await _factory.ResetDatabaseAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var studentCode = $"PG-DUP-{unique}";

        using (var scope = _factory.CreateDbContextScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await CreateStudentAsync(dbContext, studentCode, $"pg.dup.one.{unique}@school.test");
            await dbContext.SaveChangesAsync();

            await CreateStudentAsync(dbContext, studentCode, $"pg.dup.two.{unique}@school.test");

            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        }

        using var verificationScope = _factory.CreateDbContextScope();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var studentCount = await verificationDbContext.Students.CountAsync(x => x.StudentCode == studentCode);

        Assert.Equal(1, studentCount);
    }

    private async Task<string?> GetIndexDefinitionAsync(string indexName)
    {
        using var scope = _factory.CreateDbContextScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connection = dbContext.Database.GetDbConnection();
        await dbContext.Database.OpenConnectionAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT indexdef
                FROM pg_indexes
                WHERE schemaname = 'public'
                  AND indexname = @indexName;
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "indexName";
            parameter.Value = indexName;
            command.Parameters.Add(parameter);

            return await command.ExecuteScalarAsync() as string;
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static async Task<Student> CreateStudentAsync(AppDbContext dbContext, string studentCode, string email)
    {
        var studentRole = await dbContext.Roles.SingleAsync(x => x.Name == "Student");
        var student = new Student
        {
            StudentCode = studentCode,
            DateOfBirth = new DateOnly(2012, 1, 1),
            Gender = Gender.Other,
            AdmissionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            User = new User
            {
                RoleId = studentRole.Id,
                FullName = $"PostgreSQL Student {studentCode}",
                Email = email,
                PasswordHash = "test-password-hash",
                IsActive = true
            }
        };

        dbContext.Students.Add(student);
        return student;
    }
}
