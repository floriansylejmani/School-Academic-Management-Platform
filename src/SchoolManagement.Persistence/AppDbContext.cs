using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<AcademicClass> AcademicClasses => Set<AcademicClass>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherSubjectAssignment> TeacherSubjectAssignments => Set<TeacherSubjectAssignment>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionAIReview> SubmissionAIReviews => Set<SubmissionAIReview>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Fee> Fees => Set<Fee>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ResetToken> ResetTokens => Set<ResetToken>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Address).HasMaxLength(250);
            entity.Property(x => x.RefreshToken).HasMaxLength(500);
            entity.Property(x => x.ProfilePictureUrl).HasMaxLength(500);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Parent>(entity =>
        {
            entity.Property(x => x.Occupation).HasMaxLength(100);
            entity.HasOne(x => x.User)
                .WithOne(x => x.ParentProfile)
                .HasForeignKey<Parent>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasIndex(x => x.TeacherCode).IsUnique();
            entity.Property(x => x.TeacherCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Specialization).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.User)
                .WithOne(x => x.TeacherProfile)
                .HasForeignKey<Teacher>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasIndex(x => x.StudentCode).IsUnique();
            entity.Property(x => x.StudentCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(x => x.User)
                .WithOne(x => x.StudentProfile)
                .HasForeignKey<Student>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Students)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Class)
                .WithMany(x => x.Students)
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AcademicClass>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Section).HasMaxLength(20).IsRequired();
            entity.Property(x => x.AcademicYear).HasMaxLength(20).IsRequired();
            entity.HasOne(x => x.ClassTeacher)
                .WithMany(x => x.HomeroomClasses)
                .HasForeignKey(x => x.ClassTeacherId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<TeacherSubjectAssignment>(entity =>
        {
            entity.HasIndex(x => new { x.TeacherId, x.SubjectId, x.ClassId }).IsUnique();
            entity.HasOne(x => x.Teacher).WithMany(x => x.SubjectAssignments).HasForeignKey(x => x.TeacherId);
            entity.HasOne(x => x.Subject).WithMany(x => x.TeacherAssignments).HasForeignKey(x => x.SubjectId);
            entity.HasOne(x => x.Class).WithMany(x => x.SubjectAssignments).HasForeignKey(x => x.ClassId);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.Property(x => x.AcademicYear).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasOne(x => x.Student).WithMany(x => x.Enrollments).HasForeignKey(x => x.StudentId);
            entity.HasOne(x => x.Class).WithMany(x => x.Enrollments).HasForeignKey(x => x.ClassId);
        });

        modelBuilder.Entity<TimetableEntry>(entity =>
        {
            entity.Property(x => x.RoomNumber).HasMaxLength(30);
            entity.ToTable(t => t.HasCheckConstraint("CK_TimetableEntry_EndTime", "\"EndTime\" > \"StartTime\""));
            entity.HasOne(x => x.Class).WithMany(x => x.TimetableEntries).HasForeignKey(x => x.ClassId);
            entity.HasOne(x => x.Subject).WithMany(x => x.TimetableEntries).HasForeignKey(x => x.SubjectId);
            entity.HasOne(x => x.Teacher).WithMany(x => x.TimetableEntries).HasForeignKey(x => x.TeacherId);
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasIndex(x => x.Date);
            entity.HasIndex(x => new { x.StudentId, x.ClassId, x.Date });
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Remarks).HasMaxLength(250);
            entity.HasOne(x => x.Student).WithMany(x => x.AttendanceRecords).HasForeignKey(x => x.StudentId);
            entity.HasOne(x => x.Class).WithMany(x => x.AttendanceRecords).HasForeignKey(x => x.ClassId);
            entity.HasOne(x => x.Subject).WithMany(x => x.AttendanceRecords).HasForeignKey(x => x.SubjectId);
            entity.HasOne(x => x.Teacher).WithMany(x => x.AttendanceRecords).HasForeignKey(x => x.TeacherId);
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(100).IsRequired();
            entity.Property(x => x.TotalMarks).HasPrecision(10, 2);
            entity.ToTable(t => t.HasCheckConstraint("CK_Exam_TotalMarks", "\"TotalMarks\" > 0"));
            entity.HasOne(x => x.Class).WithMany(x => x.Exams).HasForeignKey(x => x.ClassId);
            entity.HasOne(x => x.Subject).WithMany(x => x.Exams).HasForeignKey(x => x.SubjectId);
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique();
            entity.Property(x => x.EssayPrompt).HasMaxLength(2000);
            entity.Property(x => x.AnswerText).HasMaxLength(20000).IsRequired();
            entity.Property(x => x.MaximumScore).HasPrecision(10, 2);
            entity.Property(x => x.TeacherFinalScore).HasPrecision(10, 2);
            entity.Property(x => x.TeacherFinalGrade).HasMaxLength(20);
            entity.Property(x => x.TeacherReviewNotes).HasMaxLength(2000);
            entity.HasOne(x => x.Exam)
                .WithMany(x => x.Submissions)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Student)
                .WithMany(x => x.Submissions)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SubmittedByUser)
                .WithMany(x => x.SubmittedSubmissions)
                .HasForeignKey(x => x.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReviewedByUser)
                .WithMany(x => x.ReviewedSubmissions)
                .HasForeignKey(x => x.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SubmissionAIReview>(entity =>
        {
            entity.HasIndex(x => x.SubmissionId).IsUnique();
            entity.Property(x => x.Mode).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ProviderResponseId).HasMaxLength(150);
            entity.Property(x => x.OverallSuggestedScore).HasPrecision(10, 2);
            entity.Property(x => x.SummaryFeedback).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.StrengthsJson).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.WeaknessesJson).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.ImprovementsJson).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.RubricBreakdownJson).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.SafetyNotes).HasMaxLength(2000);
            entity.HasOne(x => x.Submission)
                .WithOne(x => x.AIReview)
                .HasForeignKey<SubmissionAIReview>(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.RequestedByUser)
                .WithMany(x => x.RequestedSubmissionAIReviews)
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.Property(x => x.MarksObtained).HasPrecision(10, 2);
            entity.Property(x => x.Grade).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(250);
            entity.HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique();
            entity.HasOne(x => x.Exam).WithMany(x => x.Results).HasForeignKey(x => x.ExamId);
            entity.HasOne(x => x.Student).WithMany(x => x.Results).HasForeignKey(x => x.StudentId);
        });

        modelBuilder.Entity<Fee>(entity =>
        {
            entity.HasIndex(x => x.StudentId);
            entity.HasIndex(x => new { x.Status, x.DueDate });
            entity.Property(x => x.FeeType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(10, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.ToTable(t => t.HasCheckConstraint("CK_Fee_Amount", "\"Amount\" >= 0"));
            entity.HasOne(x => x.Student).WithMany(x => x.Fees).HasForeignKey(x => x.StudentId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.Property(x => x.AmountPaid).HasPrecision(10, 2);
            entity.Property(x => x.TransactionReference).HasMaxLength(100);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(100);
            entity.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);
            entity.ToTable(t => t.HasCheckConstraint("CK_Payment_AmountPaid", "\"AmountPaid\" > 0"));
            entity.HasOne(x => x.Fee).WithMany(x => x.Payments).HasForeignKey(x => x.FeeId);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.StudentId);
            entity.Property(x => x.Title).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(500).IsRequired();
            entity.HasOne(x => x.User).WithMany(x => x.Notifications).HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ResetToken>(entity =>
        {
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasOne(x => x.User)
                .WithMany(x => x.ResetTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UploadedFile>(entity =>
        {
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.Property(x => x.StoredFileName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            // Performance indexes
            entity.HasIndex(x => x.Timestamp);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.ActionType);
            entity.HasIndex(x => x.RequestPath);
            entity.HasIndex(x => x.IsSensitive);
            entity.HasIndex(x => new { x.Timestamp, x.UserId });
            entity.HasIndex(x => new { x.Timestamp, x.ActionType });
            entity.HasIndex(x => new { x.Timestamp, x.IsSensitive });
            entity.HasIndex(x => x.TraceId);

            // Property configurations
            entity.Property(x => x.UserEmail).HasMaxLength(150);
            entity.Property(x => x.IpAddress).HasMaxLength(45); // IPv6 support
            entity.Property(x => x.UserAgent).HasMaxLength(500);
            entity.Property(x => x.RequestPath).HasMaxLength(2048);
            entity.Property(x => x.QueryString).HasMaxLength(2048);
            entity.Property(x => x.ActionType).HasMaxLength(50);
            entity.Property(x => x.TraceId).HasMaxLength(100);

            // Relationship configuration
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Table configuration
            entity.ToTable("AuditLogs");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesToUtc();

        var trackedEntries = ChangeTracker.Entries()
            .Where(x => x.Entity is not null &&
                        x.Entity.GetType().IsAssignableTo(typeof(SchoolManagement.Domain.Common.BaseEntity)) &&
                        x.State is EntityState.Modified);

        foreach (var entry in trackedEntries)
        {
            ((SchoolManagement.Domain.Common.BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        NormalizeDateTimesToUtc();

        var trackedEntries = ChangeTracker.Entries()
            .Where(x => x.Entity is not null &&
                        x.Entity.GetType().IsAssignableTo(typeof(SchoolManagement.Domain.Common.BaseEntity)) &&
                        x.State is EntityState.Modified);

        foreach (var entry in trackedEntries)
        {
            ((SchoolManagement.Domain.Common.BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChanges();
    }

    private void NormalizeDateTimesToUtc()
    {
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            foreach (var property in entry.Properties)
            {
                if (property.Metadata.ClrType == typeof(DateTime) && property.CurrentValue is DateTime dateTime)
                {
                    property.CurrentValue = NormalizeToUtc(dateTime);
                    continue;
                }

                if (property.Metadata.ClrType == typeof(DateTime?) && property.CurrentValue is DateTime nullableDateTime)
                {
                    property.CurrentValue = NormalizeToUtc(nullableDateTime);
                }
            }
        }
    }

    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
}
