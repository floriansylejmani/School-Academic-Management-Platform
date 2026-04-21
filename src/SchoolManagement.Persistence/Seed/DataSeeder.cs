using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Persistence.Seed;

public static class DataSeeder
{
    private static readonly DateTime DemoReferenceUtc = new(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly SubjectSeed[] SubjectSeeds =
    [
        new("Mathematics", "MATH", "Advanced mathematics including calculus and algebra"),
        new("English", "ENG", "English language and literature"),
        new("Physics", "PHY", "Physics and physical sciences"),
        new("Chemistry", "CHEM", "Chemistry and chemical sciences"),
        new("Biology", "BIO", "Biology and life sciences"),
        new("History", "HIST", "World and local history"),
        new("Geography", "GEO", "Geography and earth sciences"),
        new("Computer Science", "CS", "Programming and computer science fundamentals")
    ];

    private static readonly AcademicClassSeed[] AcademicClassSeeds =
    [
        new("Grade 10", "A", "2024-2025"),
        new("Grade 10", "B", "2024-2025"),
        new("Grade 11", "A", "2024-2025")
    ];

    private static readonly TeacherSeed[] TeacherSeeds =
    [
        new("Dr. Sarah Johnson", "sarah.johnson@school.com", "Mathematics", "T001"),
        new("Mr. Michael Chen", "michael.chen@school.com", "English", "T002"),
        new("Ms. Emily Rodriguez", "emily.rodriguez@school.com", "Physics", "T003"),
        new("Dr. James Wilson", "james.wilson@school.com", "Chemistry", "T004")
    ];

    private static readonly AssignmentSeed[] AssignmentSeeds =
    [
        new("T001", "MATH", ClassKey("Grade 10", "A", "2024-2025")),
        new("T001", "MATH", ClassKey("Grade 10", "B", "2024-2025")),
        new("T001", "MATH", ClassKey("Grade 11", "A", "2024-2025")),
        new("T002", "ENG", ClassKey("Grade 10", "A", "2024-2025")),
        new("T002", "ENG", ClassKey("Grade 10", "B", "2024-2025")),
        new("T002", "ENG", ClassKey("Grade 11", "A", "2024-2025")),
        new("T003", "PHY", ClassKey("Grade 10", "A", "2024-2025")),
        new("T003", "PHY", ClassKey("Grade 11", "A", "2024-2025")),
        new("T004", "CHEM", ClassKey("Grade 10", "B", "2024-2025")),
        new("T004", "CHEM", ClassKey("Grade 11", "A", "2024-2025")),
        new("T001", "BIO", ClassKey("Grade 10", "A", "2024-2025")),
        new("T002", "HIST", ClassKey("Grade 10", "B", "2024-2025")),
        new("T003", "GEO", ClassKey("Grade 11", "A", "2024-2025")),
        new("T004", "CS", ClassKey("Grade 10", "A", "2024-2025"))
    ];

    private static readonly ParentSeed[] ParentSeeds =
    [
        new("Robert Martinez", "robert.martinez@email.com", "Software Engineer"),
        new("Lisa Martinez", "lisa.martinez@email.com", "Teacher"),
        new("David Kim", "david.kim@email.com", "Doctor"),
        new("Jennifer Kim", "jennifer.kim@email.com", "Nurse"),
        new("Mark Thompson", "mark.thompson@email.com", "Business Analyst"),
        new("Anna Thompson", "anna.thompson@email.com", "Graphic Designer"),
        new("John Davis", "john.davis@email.com", "Accountant"),
        new("Maria Davis", "maria.davis@email.com", "Lawyer")
    ];

    private static readonly StudentSeed[] StudentSeeds =
    [
        new("Alex Martinez", "alex.martinez@student.school.com", "robert.martinez@email.com", ClassKey("Grade 10", "A", "2024-2025"), "S001", new DateOnly(2008, 5, 15), Gender.Male),
        new("Sophie Martinez", "sophie.martinez@student.school.com", "robert.martinez@email.com", ClassKey("Grade 10", "A", "2024-2025"), "S002", new DateOnly(2008, 8, 22), Gender.Female),
        new("Ryan Kim", "ryan.kim@student.school.com", "david.kim@email.com", ClassKey("Grade 10", "A", "2024-2025"), "S003", new DateOnly(2008, 3, 10), Gender.Male),
        new("Emma Kim", "emma.kim@student.school.com", "david.kim@email.com", ClassKey("Grade 10", "A", "2024-2025"), "S004", new DateOnly(2008, 11, 5), Gender.Female),
        new("Jordan Lee", "jordan.lee@student.school.com", "mark.thompson@email.com", ClassKey("Grade 10", "A", "2024-2025"), "S005", new DateOnly(2008, 7, 18), Gender.Other),
        new("Taylor Brown", "taylor.brown@student.school.com", "john.davis@email.com", ClassKey("Grade 10", "A", "2024-2025"), "S006", new DateOnly(2008, 1, 30), Gender.Female),
        new("Casey Wilson", "casey.wilson@student.school.com", "lisa.martinez@email.com", ClassKey("Grade 10", "A", "2024-2025"), "S007", new DateOnly(2008, 9, 12), Gender.Other),
        new("Morgan Davis", "morgan.davis@student.school.com", "maria.davis@email.com", ClassKey("Grade 10", "A", "2024-2025"), "S008", new DateOnly(2008, 4, 25), Gender.Female),
        new("Jamie Thompson", "jamie.thompson@student.school.com", "mark.thompson@email.com", ClassKey("Grade 10", "B", "2024-2025"), "S009", new DateOnly(2008, 6, 8), Gender.Other),
        new("Avery Garcia", "avery.garcia@student.school.com", "anna.thompson@email.com", ClassKey("Grade 10", "B", "2024-2025"), "S010", new DateOnly(2008, 12, 3), Gender.Female),
        new("Cameron Rodriguez", "cameron.rodriguez@student.school.com", "jennifer.kim@email.com", ClassKey("Grade 10", "B", "2024-2025"), "S011", new DateOnly(2008, 2, 14), Gender.Male),
        new("Riley Johnson", "riley.johnson@student.school.com", "lisa.martinez@email.com", ClassKey("Grade 10", "B", "2024-2025"), "S012", new DateOnly(2008, 10, 20), Gender.Female),
        new("Skyler White", "skyler.white@student.school.com", "john.davis@email.com", ClassKey("Grade 10", "B", "2024-2025"), "S013", new DateOnly(2008, 7, 7), Gender.Other),
        new("Peyton Hall", "peyton.hall@student.school.com", "maria.davis@email.com", ClassKey("Grade 10", "B", "2024-2025"), "S014", new DateOnly(2008, 5, 28), Gender.Male),
        new("Logan Young", "logan.young@student.school.com", "david.kim@email.com", ClassKey("Grade 10", "B", "2024-2025"), "S015", new DateOnly(2008, 8, 16), Gender.Male),
        new("Dakota King", "dakota.king@student.school.com", "jennifer.kim@email.com", ClassKey("Grade 10", "B", "2024-2025"), "S016", new DateOnly(2008, 11, 9), Gender.Female),
        new("Reese Scott", "reese.scott@student.school.com", "anna.thompson@email.com", ClassKey("Grade 11", "A", "2024-2025"), "S017", new DateOnly(2007, 4, 12), Gender.Other),
        new("Charlie Adams", "charlie.adams@student.school.com", "robert.martinez@email.com", ClassKey("Grade 11", "A", "2024-2025"), "S018", new DateOnly(2007, 9, 5), Gender.Male),
        new("Quinn Baker", "quinn.baker@student.school.com", "lisa.martinez@email.com", ClassKey("Grade 11", "A", "2024-2025"), "S019", new DateOnly(2007, 1, 23), Gender.Other),
        new("Rowan Carter", "rowan.carter@student.school.com", "jennifer.kim@email.com", ClassKey("Grade 11", "A", "2024-2025"), "S020", new DateOnly(2007, 6, 30), Gender.Female),
        new("Sage Diaz", "sage.diaz@student.school.com", "mark.thompson@email.com", ClassKey("Grade 11", "A", "2024-2025"), "S021", new DateOnly(2007, 3, 17), Gender.Other),
        new("Finley Evans", "finley.evans@student.school.com", "john.davis@email.com", ClassKey("Grade 11", "A", "2024-2025"), "S022", new DateOnly(2007, 8, 8), Gender.Male),
        new("Harper Foster", "harper.foster@student.school.com", "maria.davis@email.com", ClassKey("Grade 11", "A", "2024-2025"), "S023", new DateOnly(2007, 12, 1), Gender.Female),
        new("Ellis Green", "ellis.green@student.school.com", "david.kim@email.com", ClassKey("Grade 11", "A", "2024-2025"), "S024", new DateOnly(2007, 7, 14), Gender.Other)
    ];

    private static readonly ExamSeed[] ExamSeeds =
    [
        new(ClassKey("Grade 10", "A", "2024-2025"), "MATH", "Mathematics Mid-term Exam", new DateOnly(2024, 3, 15), 100m),
        new(ClassKey("Grade 10", "A", "2024-2025"), "MATH", "Mathematics Final Exam", new DateOnly(2024, 6, 10), 100m),
        new(ClassKey("Grade 10", "B", "2024-2025"), "MATH", "Mathematics Mid-term Exam", new DateOnly(2024, 3, 15), 100m),
        new(ClassKey("Grade 10", "B", "2024-2025"), "MATH", "Mathematics Final Exam", new DateOnly(2024, 6, 10), 100m),
        new(ClassKey("Grade 11", "A", "2024-2025"), "MATH", "Mathematics Mid-term Exam", new DateOnly(2024, 3, 15), 100m),
        new(ClassKey("Grade 11", "A", "2024-2025"), "MATH", "Mathematics Final Exam", new DateOnly(2024, 6, 10), 100m),
        new(ClassKey("Grade 10", "A", "2024-2025"), "ENG", "English Literature Quiz", new DateOnly(2024, 2, 20), 50m),
        new(ClassKey("Grade 10", "A", "2024-2025"), "ENG", "English Mid-term Exam", new DateOnly(2024, 3, 20), 100m),
        new(ClassKey("Grade 10", "B", "2024-2025"), "ENG", "English Literature Quiz", new DateOnly(2024, 2, 20), 50m),
        new(ClassKey("Grade 10", "B", "2024-2025"), "ENG", "English Mid-term Exam", new DateOnly(2024, 3, 20), 100m),
        new(ClassKey("Grade 11", "A", "2024-2025"), "ENG", "English Literature Quiz", new DateOnly(2024, 2, 20), 50m),
        new(ClassKey("Grade 11", "A", "2024-2025"), "ENG", "English Mid-term Exam", new DateOnly(2024, 3, 20), 100m),
        new(ClassKey("Grade 10", "A", "2024-2025"), "PHY", "Physics Mid-term Practical", new DateOnly(2024, 3, 25), 75m),
        new(ClassKey("Grade 10", "A", "2024-2025"), "PHY", "Physics Final Exam", new DateOnly(2024, 6, 15), 100m),
        new(ClassKey("Grade 11", "A", "2024-2025"), "PHY", "Physics Mid-term Practical", new DateOnly(2024, 3, 25), 75m),
        new(ClassKey("Grade 11", "A", "2024-2025"), "PHY", "Physics Final Exam", new DateOnly(2024, 6, 15), 100m),
        new(ClassKey("Grade 10", "B", "2024-2025"), "CHEM", "Chemistry Lab Quiz", new DateOnly(2024, 2, 25), 50m),
        new(ClassKey("Grade 10", "B", "2024-2025"), "CHEM", "Chemistry Mid-term Exam", new DateOnly(2024, 3, 30), 100m),
        new(ClassKey("Grade 11", "A", "2024-2025"), "CHEM", "Chemistry Lab Quiz", new DateOnly(2024, 2, 25), 50m),
        new(ClassKey("Grade 11", "A", "2024-2025"), "CHEM", "Chemistry Mid-term Exam", new DateOnly(2024, 3, 30), 100m)
    ];

    private static readonly string[] NotificationTemplates =
    [
        "Your {0} exam results are now available. Check your grades in the results section.",
        "Attendance marked for {0} class on {1}. Status: {2}",
        "Fee payment of ${3} received for {4}. Thank you!",
        "New assignment posted for {0} subject. Due date: {1}",
        "Parent-teacher meeting scheduled for {1}. Please confirm attendance.",
        "School event: {0} on {1}. Don't miss out!",
        "Report card generated for {0}. Available for download.",
        "Timetable updated. Check your schedule for changes."
    ];

    public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var roles = await EnsureRolesAsync(context, cancellationToken);
        var adminUser = await EnsureAdminUserAsync(context, passwordHasher, roles["Admin"].Id, cancellationToken);

        var subjects = await EnsureSubjectsAsync(context, cancellationToken);
        var classes = await EnsureClassesAsync(context, cancellationToken);
        var teachers = await EnsureTeachersAsync(context, passwordHasher, roles["Teacher"].Id, cancellationToken);
        await EnsureClassTeacherAssignmentsAsync(context, classes, teachers, cancellationToken);

        var assignments = await EnsureTeacherSubjectAssignmentsAsync(context, teachers, subjects, classes, cancellationToken);
        var parents = await EnsureParentsAsync(context, passwordHasher, roles["Parent"].Id, cancellationToken);
        var students = await EnsureStudentsAsync(context, passwordHasher, roles["Student"].Id, parents, classes, cancellationToken);

        await EnsureEnrollmentsAsync(context, students, cancellationToken);
        var timetableEntries = await EnsureTimetableEntriesAsync(context, assignments, classes, cancellationToken);
        await EnsureAttendanceRecordsAsync(context, students, timetableEntries, cancellationToken);

        var exams = await EnsureExamsAsync(context, subjects, classes, cancellationToken);
        await EnsureResultsAsync(context, exams, students, cancellationToken);

        var fees = await EnsureFeesAsync(context, students, cancellationToken);
        await EnsurePaymentsAsync(context, fees, cancellationToken);
        await EnsureNotificationsAsync(context, adminUser, teachers, students, parents, subjects, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, Role>> EnsureRolesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var roleNames = new[] { "Admin", "Teacher", "Student", "Parent" };
        var existingRoles = await context.Roles
            .Where(x => roleNames.Contains(x.Name))
            .ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var roleName in roleNames)
        {
            if (existingRoles.ContainsKey(roleName))
            {
                continue;
            }

            var role = new Role { Name = roleName };
            context.Roles.Add(role);
            existingRoles[roleName] = role;
        }

        await context.SaveChangesAsync(cancellationToken);
        return existingRoles;
    }

    private static async Task<User> EnsureAdminUserAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        Guid adminRoleId,
        CancellationToken cancellationToken)
    {
        var adminUser = await context.Users.SingleOrDefaultAsync(x => x.Email == "admin@school.com", cancellationToken);
        if (adminUser is not null)
        {
            return adminUser;
        }

        adminUser = new User
        {
            RoleId = adminRoleId,
            FullName = "System Admin",
            Email = "admin@school.com",
            PasswordHash = passwordHasher.HashPassword("Admin@12345"),
            IsActive = true
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync(cancellationToken);
        return adminUser;
    }

    private static async Task<Dictionary<string, Subject>> EnsureSubjectsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var codes = SubjectSeeds.Select(x => x.Code).ToArray();
        var subjectsByCode = await context.Subjects
            .Where(x => codes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var seed in SubjectSeeds)
        {
            if (subjectsByCode.ContainsKey(seed.Code))
            {
                continue;
            }

            var subject = new Subject
            {
                Name = seed.Name,
                Code = seed.Code,
                Description = seed.Description
            };

            context.Subjects.Add(subject);
            subjectsByCode[seed.Code] = subject;
        }

        await context.SaveChangesAsync(cancellationToken);
        return subjectsByCode;
    }

    private static async Task<Dictionary<string, AcademicClass>> EnsureClassesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var academicYear = AcademicClassSeeds.Select(x => x.AcademicYear).Distinct().ToArray();
        var classes = await context.AcademicClasses
            .Where(x => academicYear.Contains(x.AcademicYear))
            .ToListAsync(cancellationToken);

        var classesByKey = classes.ToDictionary(ClassKey, StringComparer.OrdinalIgnoreCase);
        foreach (var seed in AcademicClassSeeds)
        {
            var key = ClassKey(seed.Name, seed.Section, seed.AcademicYear);
            if (classesByKey.ContainsKey(key))
            {
                continue;
            }

            var classEntity = new AcademicClass
            {
                Name = seed.Name,
                Section = seed.Section,
                AcademicYear = seed.AcademicYear
            };

            context.AcademicClasses.Add(classEntity);
            classesByKey[key] = classEntity;
        }

        await context.SaveChangesAsync(cancellationToken);
        return classesByKey;
    }

    private static async Task<Dictionary<string, Teacher>> EnsureTeachersAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        Guid teacherRoleId,
        CancellationToken cancellationToken)
    {
        var emails = TeacherSeeds.Select(x => x.Email).ToArray();
        var codes = TeacherSeeds.Select(x => x.Code).ToArray();

        var usersByEmail = await context.Users
            .Where(x => emails.Contains(x.Email))
            .ToDictionaryAsync(x => x.Email, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var teachersByCode = await context.Teachers
            .Where(x => codes.Contains(x.TeacherCode))
            .ToDictionaryAsync(x => x.TeacherCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var (seed, index) in TeacherSeeds.Select((seed, index) => (seed, index)))
        {
            if (!usersByEmail.TryGetValue(seed.Email, out var user))
            {
                user = new User
                {
                    RoleId = teacherRoleId,
                    FullName = seed.FullName,
                    Email = seed.Email,
                    PasswordHash = passwordHasher.HashPassword("Teacher@123"),
                    Phone = $"+1-555-{1000 + index:D4}",
                    Address = $"123 Education St, Suite {index + 1}, Academic City, AC 12345",
                    IsActive = true
                };

                context.Users.Add(user);
                await context.SaveChangesAsync(cancellationToken);
                usersByEmail[seed.Email] = user;
            }

            if (teachersByCode.ContainsKey(seed.Code))
            {
                continue;
            }

            var teacher = new Teacher
            {
                UserId = user.Id,
                TeacherCode = seed.Code,
                Specialization = seed.Specialization,
                HireDate = DateOnly.FromDateTime(DemoReferenceUtc.AddYears(-2).AddMonths(-index))
            };

            context.Teachers.Add(teacher);
            await context.SaveChangesAsync(cancellationToken);
            teachersByCode[seed.Code] = teacher;
        }

        return teachersByCode;
    }

    private static async Task EnsureClassTeacherAssignmentsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, AcademicClass> classes,
        IReadOnlyDictionary<string, Teacher> teachers,
        CancellationToken cancellationToken)
    {
        var assignments = new[]
        {
            (ClassKey("Grade 10", "A", "2024-2025"), "T001"),
            (ClassKey("Grade 10", "B", "2024-2025"), "T002"),
            (ClassKey("Grade 11", "A", "2024-2025"), "T003")
        };

        var hasChanges = false;
        foreach (var (classKey, teacherCode) in assignments)
        {
            var classEntity = classes[classKey];
            var teacher = teachers[teacherCode];
            if (classEntity.ClassTeacherId == teacher.Id)
            {
                continue;
            }

            classEntity.ClassTeacherId = teacher.Id;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<List<AssignmentRef>> EnsureTeacherSubjectAssignmentsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, Teacher> teachers,
        IReadOnlyDictionary<string, Subject> subjects,
        IReadOnlyDictionary<string, AcademicClass> classes,
        CancellationToken cancellationToken)
    {
        var existingKeys = (await context.TeacherSubjectAssignments
                .Select(x => new { x.TeacherId, x.SubjectId, x.ClassId })
                .ToListAsync(cancellationToken))
            .Select(x => (x.TeacherId, x.SubjectId, x.ClassId))
            .ToHashSet();

        var missingAssignments = new List<TeacherSubjectAssignment>();
        foreach (var seed in AssignmentSeeds)
        {
            var key = (teachers[seed.TeacherCode].Id, subjects[seed.SubjectCode].Id, classes[seed.ClassKey].Id);
            if (existingKeys.Contains(key))
            {
                continue;
            }

            missingAssignments.Add(new TeacherSubjectAssignment
            {
                TeacherId = key.Item1,
                SubjectId = key.Item2,
                ClassId = key.Item3
            });
        }

        if (missingAssignments.Count > 0)
        {
            context.TeacherSubjectAssignments.AddRange(missingAssignments);
            await context.SaveChangesAsync(cancellationToken);
        }

        return AssignmentSeeds
            .Select(seed => new AssignmentRef(
                teachers[seed.TeacherCode].Id,
                subjects[seed.SubjectCode].Id,
                classes[seed.ClassKey].Id))
            .ToList();
    }

    private static async Task<Dictionary<string, Parent>> EnsureParentsAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        Guid parentRoleId,
        CancellationToken cancellationToken)
    {
        var emails = ParentSeeds.Select(x => x.Email).ToArray();
        var usersByEmail = await context.Users
            .Where(x => emails.Contains(x.Email))
            .ToDictionaryAsync(x => x.Email, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var parentsByEmail = await context.Parents
            .Include(x => x.User)
            .Where(x => x.User != null && emails.Contains(x.User.Email))
            .ToDictionaryAsync(x => x.User!.Email, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var (seed, index) in ParentSeeds.Select((seed, index) => (seed, index)))
        {
            if (!usersByEmail.TryGetValue(seed.Email, out var user))
            {
                user = new User
                {
                    RoleId = parentRoleId,
                    FullName = seed.FullName,
                    Email = seed.Email,
                    PasswordHash = passwordHasher.HashPassword("Parent@123"),
                    Phone = $"+1-555-{2000 + index:D4}",
                    Address = $"456 Parent Ave, Unit {index + 1}, Family Town, FT 67890",
                    IsActive = true
                };

                context.Users.Add(user);
                await context.SaveChangesAsync(cancellationToken);
                usersByEmail[seed.Email] = user;
            }

            if (parentsByEmail.ContainsKey(seed.Email))
            {
                continue;
            }

            var parent = new Parent
            {
                UserId = user.Id,
                Occupation = seed.Occupation
            };

            context.Parents.Add(parent);
            await context.SaveChangesAsync(cancellationToken);
            parentsByEmail[seed.Email] = parent;
        }

        return parentsByEmail;
    }

    private static async Task<Dictionary<string, Student>> EnsureStudentsAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        Guid studentRoleId,
        IReadOnlyDictionary<string, Parent> parents,
        IReadOnlyDictionary<string, AcademicClass> classes,
        CancellationToken cancellationToken)
    {
        var emails = StudentSeeds.Select(x => x.Email).ToArray();
        var codes = StudentSeeds.Select(x => x.Code).ToArray();

        var usersByEmail = await context.Users
            .Where(x => emails.Contains(x.Email))
            .ToDictionaryAsync(x => x.Email, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var studentsByCode = await context.Students
            .Where(x => codes.Contains(x.StudentCode))
            .ToDictionaryAsync(x => x.StudentCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var (seed, index) in StudentSeeds.Select((seed, index) => (seed, index)))
        {
            if (!usersByEmail.TryGetValue(seed.Email, out var user))
            {
                user = new User
                {
                    RoleId = studentRoleId,
                    FullName = seed.FullName,
                    Email = seed.Email,
                    PasswordHash = passwordHasher.HashPassword("Student@123"),
                    Phone = $"+1-555-{3000 + index:D4}",
                    Address = $"789 Student Blvd, Room {index + 1}, Learning City, LC 45678",
                    IsActive = true
                };

                context.Users.Add(user);
                await context.SaveChangesAsync(cancellationToken);
                usersByEmail[seed.Email] = user;
            }

            if (studentsByCode.ContainsKey(seed.Code))
            {
                continue;
            }

            var student = new Student
            {
                UserId = user.Id,
                ParentId = parents[seed.ParentEmail].Id,
                ClassId = classes[seed.ClassKey].Id,
                StudentCode = seed.Code,
                DateOfBirth = seed.DateOfBirth,
                Gender = seed.Gender,
                AdmissionDate = new DateOnly(2023, 8, 15)
            };

            context.Students.Add(student);
            await context.SaveChangesAsync(cancellationToken);
            studentsByCode[seed.Code] = student;
        }

        return studentsByCode;
    }

    private static async Task EnsureEnrollmentsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, Student> students,
        CancellationToken cancellationToken)
    {
        var studentIds = students.Values.Select(x => x.Id).ToArray();
        var existingKeys = (await context.Enrollments
                .Where(x => studentIds.Contains(x.StudentId))
                .Select(x => new { x.StudentId, x.ClassId, x.AcademicYear })
                .ToListAsync(cancellationToken))
            .Select(x => (x.StudentId, x.ClassId, x.AcademicYear))
            .ToHashSet();

        var enrollments = StudentSeeds
            .Select(seed => students[seed.Code])
            .Where(student => student.ClassId.HasValue)
            .Select(student => new Enrollment
            {
                StudentId = student.Id,
                ClassId = student.ClassId!.Value,
                AcademicYear = "2024-2025",
                EnrollmentDate = new DateOnly(2024, 8, 15),
                Status = "Active"
            })
            .Where(enrollment => !existingKeys.Contains((enrollment.StudentId, enrollment.ClassId, enrollment.AcademicYear)))
            .ToList();

        if (enrollments.Count == 0)
        {
            return;
        }

        context.Enrollments.AddRange(enrollments);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<TimetableEntry>> EnsureTimetableEntriesAsync(
        AppDbContext context,
        IReadOnlyList<AssignmentRef> assignments,
        IReadOnlyDictionary<string, AcademicClass> classes,
        CancellationToken cancellationToken)
    {
        var classIds = classes.Values.Select(x => x.Id).ToArray();
        var existingKeys = (await context.TimetableEntries
                .Where(x => classIds.Contains(x.ClassId))
                .Select(x => new { x.ClassId, x.SubjectId, x.TeacherId, x.DayOfWeek, x.StartTime, x.EndTime })
                .ToListAsync(cancellationToken))
            .Select(x => (x.ClassId, x.SubjectId, x.TeacherId, x.DayOfWeek, x.StartTime, x.EndTime))
            .ToHashSet();

        var timeSlots = new[]
        {
            (TimeOnly.Parse("08:00"), TimeOnly.Parse("08:45")),
            (TimeOnly.Parse("08:50"), TimeOnly.Parse("09:35")),
            (TimeOnly.Parse("09:40"), TimeOnly.Parse("10:25")),
            (TimeOnly.Parse("10:30"), TimeOnly.Parse("11:15")),
            (TimeOnly.Parse("11:20"), TimeOnly.Parse("12:05")),
            (TimeOnly.Parse("12:10"), TimeOnly.Parse("12:55")),
            (TimeOnly.Parse("13:00"), TimeOnly.Parse("13:45")),
            (TimeOnly.Parse("13:50"), TimeOnly.Parse("14:35"))
        };

        var daysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var desiredEntries = new List<TimetableEntry>();

        foreach (var classSeed in AcademicClassSeeds)
        {
            var classEntity = classes[ClassKey(classSeed.Name, classSeed.Section, classSeed.AcademicYear)];
            var classAssignments = assignments.Where(x => x.ClassId == classEntity.Id).ToList();
            var slotIndex = 0;

            foreach (var day in daysOfWeek)
            {
                foreach (var timeSlot in timeSlots)
                {
                    if (slotIndex >= classAssignments.Count)
                    {
                        break;
                    }

                    var assignment = classAssignments[slotIndex];
                    desiredEntries.Add(new TimetableEntry
                    {
                        ClassId = classEntity.Id,
                        SubjectId = assignment.SubjectId,
                        TeacherId = assignment.TeacherId,
                        DayOfWeek = day,
                        StartTime = timeSlot.Item1,
                        EndTime = timeSlot.Item2,
                        RoomNumber = $"Room {((int)day * 100) + (slotIndex % 10) + 101}"
                    });

                    slotIndex++;
                }

                slotIndex = 0;
            }
        }

        var missingEntries = desiredEntries
            .Where(x => !existingKeys.Contains((x.ClassId, x.SubjectId, x.TeacherId, x.DayOfWeek, x.StartTime, x.EndTime)))
            .ToList();

        if (missingEntries.Count > 0)
        {
            context.TimetableEntries.AddRange(missingEntries);
            await context.SaveChangesAsync(cancellationToken);
        }

        return await context.TimetableEntries
            .Where(x => classIds.Contains(x.ClassId))
            .ToListAsync(cancellationToken);
    }

    private static async Task EnsureAttendanceRecordsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, Student> students,
        IReadOnlyList<TimetableEntry> timetableEntries,
        CancellationToken cancellationToken)
    {
        var studentIds = students.Values.Select(x => x.Id).ToArray();
        var existingKeys = (await context.AttendanceRecords
                .Where(x => studentIds.Contains(x.StudentId))
                .Select(x => new { x.StudentId, x.ClassId, x.SubjectId, x.TeacherId, x.Date })
                .ToListAsync(cancellationToken))
            .Select(x => (x.StudentId, x.ClassId, x.SubjectId, x.TeacherId, x.Date))
            .ToHashSet();

        var attendanceRecords = new List<AttendanceRecord>();
        var orderedStudents = StudentSeeds.Select(seed => students[seed.Code]).ToList();

        for (var month = 1; month <= 6; month++)
        {
            var daysInMonth = DateTime.DaysInMonth(2024, month);
            for (var day = 1; day <= daysInMonth; day++)
            {
                var date = new DateOnly(2024, month, day);
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    continue;
                }

                foreach (var (student, studentIndex) in orderedStudents.Select((student, index) => (student, index)))
                {
                    var timetableEntry = timetableEntries.FirstOrDefault(x =>
                        x.ClassId == student.ClassId &&
                        x.DayOfWeek == date.DayOfWeek);

                    if (timetableEntry is null)
                    {
                        continue;
                    }

                    var key = (student.Id, student.ClassId!.Value, timetableEntry.SubjectId, timetableEntry.TeacherId, date);
                    if (existingKeys.Contains(key))
                    {
                        continue;
                    }

                    var statusBucket = (studentIndex * 17 + date.DayNumber * 7 + (int)date.DayOfWeek * 13) % 100;
                    var status = statusBucket switch
                    {
                        < 85 => AttendanceStatus.Present,
                        < 90 => AttendanceStatus.Late,
                        < 95 => AttendanceStatus.Excused,
                        _ => AttendanceStatus.Absent
                    };

                    attendanceRecords.Add(new AttendanceRecord
                    {
                        StudentId = student.Id,
                        ClassId = student.ClassId.Value,
                        SubjectId = timetableEntry.SubjectId,
                        TeacherId = timetableEntry.TeacherId,
                        Date = date,
                        Status = status,
                        Remarks = status == AttendanceStatus.Late
                            ? "Arrived 10 minutes late"
                            : status == AttendanceStatus.Excused
                                ? "Medical appointment"
                                : null
                    });
                }
            }
        }

        if (attendanceRecords.Count == 0)
        {
            return;
        }

        context.AttendanceRecords.AddRange(attendanceRecords);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<Exam>> EnsureExamsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, Subject> subjects,
        IReadOnlyDictionary<string, AcademicClass> classes,
        CancellationToken cancellationToken)
    {
        var classIds = classes.Values.Select(x => x.Id).ToArray();
        var subjectIds = subjects.Values.Select(x => x.Id).ToArray();

        var existingKeys = (await context.Exams
                .Where(x => classIds.Contains(x.ClassId) && subjectIds.Contains(x.SubjectId))
                .Select(x => new { x.ClassId, x.SubjectId, x.Title, x.ExamDate, x.TotalMarks })
                .ToListAsync(cancellationToken))
            .Select(x => (x.ClassId, x.SubjectId, x.Title, x.ExamDate, x.TotalMarks))
            .ToHashSet();

        var examsToAdd = ExamSeeds
            .Select(seed => new Exam
            {
                ClassId = classes[seed.ClassKey].Id,
                SubjectId = subjects[seed.SubjectCode].Id,
                Title = seed.Title,
                ExamDate = seed.ExamDate,
                TotalMarks = seed.TotalMarks
            })
            .Where(exam => !existingKeys.Contains((exam.ClassId, exam.SubjectId, exam.Title, exam.ExamDate, exam.TotalMarks)))
            .ToList();

        if (examsToAdd.Count > 0)
        {
            context.Exams.AddRange(examsToAdd);
            await context.SaveChangesAsync(cancellationToken);
        }

        var exams = await context.Exams
            .Where(x => classIds.Contains(x.ClassId) && subjectIds.Contains(x.SubjectId))
            .ToListAsync(cancellationToken);

        var examLookup = exams.ToDictionary(
            x => (x.ClassId, x.SubjectId, x.Title, x.ExamDate, x.TotalMarks));

        return ExamSeeds
            .Select(seed => examLookup[(classes[seed.ClassKey].Id, subjects[seed.SubjectCode].Id, seed.Title, seed.ExamDate, seed.TotalMarks)])
            .ToList();
    }

    private static async Task EnsureResultsAsync(
        AppDbContext context,
        IReadOnlyList<Exam> exams,
        IReadOnlyDictionary<string, Student> students,
        CancellationToken cancellationToken)
    {
        var examIds = exams.Select(x => x.Id).ToArray();
        var existingKeys = (await context.Results
                .Where(x => examIds.Contains(x.ExamId))
                .Select(x => new { x.ExamId, x.StudentId })
                .ToListAsync(cancellationToken))
            .Select(x => (x.ExamId, x.StudentId))
            .ToHashSet();

        var orderedStudents = StudentSeeds.Select(seed => students[seed.Code]).ToList();
        var results = new List<Result>();
        var gradeRanges = new[]
        {
            (90m, "A+"),
            (80m, "A"),
            (70m, "B+"),
            (60m, "B"),
            (50m, "C"),
            (40m, "D"),
            (0m, "F")
        };

        foreach (var (exam, examIndex) in exams.Select((exam, index) => (exam, index)))
        {
            foreach (var (student, studentIndex) in orderedStudents.Where(x => x.ClassId == exam.ClassId).Select((student, index) => (student, index)))
            {
                if (existingKeys.Contains((exam.Id, student.Id)))
                {
                    continue;
                }

                var scorePercentage = 60m + ((examIndex * 11 + studentIndex * 7) % 41);
                var marks = Math.Round(exam.TotalMarks * scorePercentage / 100m, 2);
                var grade = gradeRanges.First(x => marks / exam.TotalMarks * 100m >= x.Item1).Item2;

                results.Add(new Result
                {
                    ExamId = exam.Id,
                    StudentId = student.Id,
                    MarksObtained = marks,
                    Grade = grade,
                    Remarks = grade == "A+"
                        ? "Excellent performance"
                        : grade.StartsWith("A", StringComparison.Ordinal)
                            ? "Very good work"
                            : grade.StartsWith("B", StringComparison.Ordinal)
                                ? "Good performance"
                                : grade == "F"
                                    ? "Needs improvement"
                                    : null
                });
            }
        }

        if (results.Count == 0)
        {
            return;
        }

        context.Results.AddRange(results);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<Fee>> EnsureFeesAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, Student> students,
        CancellationToken cancellationToken)
    {
        var orderedStudents = StudentSeeds.Select(seed => students[seed.Code]).ToList();
        var feeTypes = new[] { "Tuition Fee", "Library Fee", "Lab Fee", "Sports Fee", "Transportation Fee" };

        var desiredFees = new List<FeeSeed>();
        foreach (var (student, studentIndex) in orderedStudents.Select((student, index) => (student, index)))
        {
            foreach (var (feeType, feeIndex) in feeTypes.Select((feeType, index) => (feeType, index)))
            {
                var amount = feeType switch
                {
                    "Tuition Fee" => 5000m,
                    "Library Fee" => 200m,
                    "Lab Fee" => 300m,
                    "Sports Fee" => 150m,
                    "Transportation Fee" => 400m,
                    _ => 100m
                };

                desiredFees.Add(new FeeSeed(
                    student.Id,
                    feeType,
                    amount,
                    new DateOnly(2024, ((studentIndex + feeIndex) % 6) + 1, ((studentIndex * 3 + feeIndex * 5) % 27) + 1),
                    (studentIndex + feeIndex) % 5 == 0 ? FeeStatus.Pending : FeeStatus.Paid));
            }
        }

        var studentIds = orderedStudents.Select(x => x.Id).ToArray();
        var existingKeys = (await context.Fees
                .Where(x => studentIds.Contains(x.StudentId))
                .Select(x => new { x.StudentId, x.FeeType, x.Amount, x.DueDate })
                .ToListAsync(cancellationToken))
            .Select(x => (x.StudentId, x.FeeType, x.Amount, x.DueDate))
            .ToHashSet();

        var feesToAdd = desiredFees
            .Where(x => !existingKeys.Contains((x.StudentId, x.FeeType, x.Amount, x.DueDate)))
            .Select(x => new Fee
            {
                StudentId = x.StudentId,
                FeeType = x.FeeType,
                Amount = x.Amount,
                DueDate = x.DueDate,
                Status = x.Status
            })
            .ToList();

        if (feesToAdd.Count > 0)
        {
            context.Fees.AddRange(feesToAdd);
            await context.SaveChangesAsync(cancellationToken);
        }

        var fees = await context.Fees
            .Where(x => studentIds.Contains(x.StudentId))
            .ToListAsync(cancellationToken);

        var feeLookup = fees.ToDictionary(x => (x.StudentId, x.FeeType, x.Amount, x.DueDate));
        return desiredFees
            .Select(x => feeLookup[(x.StudentId, x.FeeType, x.Amount, x.DueDate)])
            .ToList();
    }

    private static async Task EnsurePaymentsAsync(AppDbContext context, IReadOnlyList<Fee> fees, CancellationToken cancellationToken)
    {
        var paymentMethods = new[] { PaymentMethod.Online, PaymentMethod.Cash, PaymentMethod.Card, PaymentMethod.BankTransfer };
        var paidFees = fees.Where(x => x.Status == FeeStatus.Paid).ToList();
        var transactionReferences = paidFees.Select(x => $"TXN-{x.Id:N}").ToArray();

        var existingReferences = await context.Payments
            .Where(x => x.TransactionReference != null && transactionReferences.Contains(x.TransactionReference))
            .Select(x => x.TransactionReference!)
            .ToListAsync(cancellationToken);

        var existingReferenceSet = existingReferences.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var payments = new List<Payment>();

        foreach (var (fee, index) in paidFees.Select((fee, index) => (fee, index)))
        {
            var transactionReference = $"TXN-{fee.Id:N}";
            if (existingReferenceSet.Contains(transactionReference))
            {
                continue;
            }

            var paymentDate = DateTime.SpecifyKind(
                fee.DueDate.ToDateTime(TimeOnly.MinValue).AddDays(-((index % 14) + 1)),
                DateTimeKind.Utc);

            payments.Add(new Payment
            {
                FeeId = fee.Id,
                AmountPaid = fee.Amount,
                PaymentDate = paymentDate,
                PaymentMethod = paymentMethods[index % paymentMethods.Length],
                TransactionReference = transactionReference
            });
        }

        if (payments.Count == 0)
        {
            return;
        }

        context.Payments.AddRange(payments);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureNotificationsAsync(
        AppDbContext context,
        User adminUser,
        IReadOnlyDictionary<string, Teacher> teachers,
        IReadOnlyDictionary<string, Student> students,
        IReadOnlyDictionary<string, Parent> parents,
        IReadOnlyDictionary<string, Subject> subjects,
        CancellationToken cancellationToken)
    {
        var notifications = new List<Notification>();
        var subjectList = SubjectSeeds.Select(seed => subjects[seed.Code]).ToList();
        var recentDates = Enumerable.Range(0, 30)
            .Select(i => DemoReferenceUtc.AddDays(-i))
            .Where(d => d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            .Take(20)
            .ToList();

        foreach (var date in recentDates.Take(5))
        {
            notifications.Add(new Notification
            {
                UserId = adminUser.Id,
                Title = "System Update",
                Message = $"Daily system check completed on {date:MMMM dd}. All services running normally.",
                IsRead = date.Day % 2 == 0
            });
        }

        foreach (var (teacher, teacherIndex) in TeacherSeeds.Select((seed, index) => (teachers[seed.Code], index)))
        {
            foreach (var (date, dateIndex) in recentDates.Take(8).Select((value, index) => (value, index)))
            {
                notifications.Add(new Notification
                {
                    UserId = teacher.UserId,
                    Title = "Academic Update",
                    Message = BuildNotificationMessage(subjectList, teacherIndex + dateIndex, date),
                    IsRead = (teacherIndex + dateIndex) % 3 != 0
                });
            }
        }

        foreach (var (student, studentIndex) in StudentSeeds.Select((seed, index) => (students[seed.Code], index)))
        {
            foreach (var (date, dateIndex) in recentDates.Take(6).Select((value, index) => (value, index)))
            {
                notifications.Add(new Notification
                {
                    UserId = student.UserId,
                    Title = "Student Update",
                    Message = BuildNotificationMessage(subjectList, studentIndex + dateIndex + 1, date),
                    IsRead = (studentIndex + dateIndex) % 2 == 0
                });
            }
        }

        foreach (var (parent, parentIndex) in ParentSeeds.Select((seed, index) => (parents[seed.Email], index)))
        {
            foreach (var (date, dateIndex) in recentDates.Take(4).Select((value, index) => (value, index)))
            {
                notifications.Add(new Notification
                {
                    UserId = parent.UserId,
                    Title = "Parent Update",
                    Message = BuildNotificationMessage(subjectList, parentIndex + dateIndex + 2, date),
                    IsRead = (parentIndex + dateIndex) % 4 == 0
                });
            }
        }

        var userIds = notifications.Select(x => x.UserId).Distinct().ToArray();
        var existingKeys = (await context.Notifications
                .Where(x => userIds.Contains(x.UserId))
                .Select(x => new { x.UserId, x.Title, x.Message })
                .ToListAsync(cancellationToken))
            .Select(x => (x.UserId, x.Title, x.Message))
            .ToHashSet();

        var notificationsToAdd = notifications
            .Where(x => !existingKeys.Contains((x.UserId, x.Title, x.Message)))
            .ToList();

        if (notificationsToAdd.Count == 0)
        {
            return;
        }

        context.Notifications.AddRange(notificationsToAdd);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string BuildNotificationMessage(IReadOnlyList<Subject> subjects, int offset, DateTime date)
    {
        var template = NotificationTemplates[offset % NotificationTemplates.Length];
        var subject = subjects[offset % subjects.Count];

        return string.Format(
            template,
            subject.Name,
            date.AddDays((offset % 6) + 1).ToString("MMMM dd"),
            "Present",
            100 + (offset % 9) * 100,
            "Tuition Fee");
    }

    private static string ClassKey(AcademicClass classEntity) => ClassKey(classEntity.Name, classEntity.Section, classEntity.AcademicYear);

    private static string ClassKey(string name, string section, string academicYear) => $"{name}|{section}|{academicYear}";

    private sealed record SubjectSeed(string Name, string Code, string Description);

    private sealed record AcademicClassSeed(string Name, string Section, string AcademicYear);

    private sealed record TeacherSeed(string FullName, string Email, string Specialization, string Code);

    private sealed record AssignmentSeed(string TeacherCode, string SubjectCode, string ClassKey);

    private sealed record ParentSeed(string FullName, string Email, string Occupation);

    private sealed record StudentSeed(
        string FullName,
        string Email,
        string ParentEmail,
        string ClassKey,
        string Code,
        DateOnly DateOfBirth,
        Gender Gender);

    private sealed record ExamSeed(string ClassKey, string SubjectCode, string Title, DateOnly ExamDate, decimal TotalMarks);

    private sealed record FeeSeed(Guid StudentId, string FeeType, decimal Amount, DateOnly DueDate, FeeStatus Status);

    private sealed record AssignmentRef(Guid TeacherId, Guid SubjectId, Guid ClassId);
}
