using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Parents;
using SchoolManagement.Application.Students;
using SchoolManagement.Application.Subjects;
using SchoolManagement.Application.Teachers;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Persistence.Common;

namespace SchoolManagement.Persistence.Services;

public sealed class StudentService : IStudentService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TeacherScopeService _teacherScopeService;

    public StudentService(AppDbContext context, IPasswordHasher passwordHasher, TeacherScopeService teacherScopeService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _teacherScopeService = teacherScopeService;
    }

    public async Task<PagedResponse<StudentResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        return await BuildStudentQuery().OrderBy(x => x.User!.FullName).ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<StudentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var student = await BuildStudentQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Student not found.", 404);

        return student.ToResponse();
    }

    public async Task<StudentResponse> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var student = await BuildStudentQuery().SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new AppException("Student profile not found.", 404);

        return student.ToResponse();
    }

    public async Task<PagedResponse<StudentResponse>> GetByParentUserIdAsync(Guid parentUserId, PaginationRequest request, CancellationToken cancellationToken)
    {
        return await BuildStudentQuery()
            .Where(x => x.Parent != null && x.Parent.UserId == parentUserId)
            .OrderBy(x => x.User!.FullName)
            .ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<PagedResponse<StudentResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);

        return await _teacherScopeService.ApplyStudentScope(BuildStudentQuery(), teacherId)
            .OrderBy(x => x.User!.FullName)
            .ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<StudentResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        var student = await _teacherScopeService.ApplyStudentScope(BuildStudentQuery(), teacherId)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Student not found.", 404);

        return student.ToResponse();
    }

    public async Task<bool> CanTeacherAccessStudentAsync(Guid teacherUserId, Guid studentId, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        return await _teacherScopeService.ApplyStudentScope(_context.Students.AsNoTracking(), teacherId)
            .AnyAsync(x => x.Id == studentId, cancellationToken);
    }

    public async Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new AppException("A user with this email already exists.");
        }

        if (await _context.Students.AnyAsync(x => x.StudentCode == request.StudentCode.Trim(), cancellationToken))
        {
            throw new AppException("Student code already exists.");
        }

        await EnsureOptionalParentAndClassExistAsync(request.ParentId, request.ClassId, cancellationToken);

        var studentRole = await _context.Roles.SingleOrDefaultAsync(x => x.Name == "Student", cancellationToken)
            ?? throw new AppException("Student role is missing. Seed data has not been applied.", 500);

        var student = new Student
        {
            User = new User
            {
                RoleId = studentRole.Id,
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Phone = request.Phone?.Trim(),
                Address = request.Address?.Trim(),
                IsActive = true
            },
            ParentId = request.ParentId,
            ClassId = request.ClassId,
            StudentCode = request.StudentCode.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            AdmissionDate = request.AdmissionDate
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(student.Id, cancellationToken);
    }

    public async Task<StudentResponse> UpdateAsync(Guid id, UpdateStudentRequest request, CancellationToken cancellationToken)
    {
        var student = await _context.Students.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Student not found.", 404);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(x => x.Id != student.UserId && x.Email == email, cancellationToken))
        {
            throw new AppException("A user with this email already exists.");
        }

        if (await _context.Students.AnyAsync(x => x.Id != id && x.StudentCode == request.StudentCode.Trim(), cancellationToken))
        {
            throw new AppException("Student code already exists.");
        }

        await EnsureOptionalParentAndClassExistAsync(request.ParentId, request.ClassId, cancellationToken);

        student.StudentCode = request.StudentCode.Trim();
        student.DateOfBirth = request.DateOfBirth;
        student.Gender = request.Gender;
        student.AdmissionDate = request.AdmissionDate;
        student.ParentId = request.ParentId;
        student.ClassId = request.ClassId;
        student.User!.FullName = request.FullName.Trim();
        student.User.Email = email;
        student.User.Phone = request.Phone?.Trim();
        student.User.Address = request.Address?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var student = await _context.Students.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Student not found.", 404);

        _context.Students.Remove(student);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Student> BuildStudentQuery()
    {
        return _context.Students.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Parent).ThenInclude(x => x!.User)
            .Include(x => x.Class);
    }

    private async Task EnsureOptionalParentAndClassExistAsync(Guid? parentId, Guid? classId, CancellationToken cancellationToken)
    {
        if (parentId.HasValue && !await _context.Parents.AnyAsync(x => x.Id == parentId.Value, cancellationToken))
        {
            throw new AppException("Parent not found.", 404);
        }

        if (classId.HasValue && !await _context.AcademicClasses.AnyAsync(x => x.Id == classId.Value, cancellationToken))
        {
            throw new AppException("Class not found.", 404);
        }
    }
}

public sealed class ParentService : IParentService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ParentService(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResponse<ParentResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        return await BuildParentQuery().OrderBy(x => x.User!.FullName).ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<ParentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var parent = await BuildParentQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Parent not found.", 404);

        return parent.ToResponse();
    }

    public async Task<ParentResponse> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var parent = await BuildParentQuery().SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new AppException("Parent profile not found.", 404);

        return parent.ToResponse();
    }

    public async Task<ParentResponse> CreateAsync(CreateParentRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new AppException("A user with this email already exists.");
        }

        var parentRole = await _context.Roles.SingleOrDefaultAsync(x => x.Name == "Parent", cancellationToken)
            ?? throw new AppException("Parent role is missing. Seed data has not been applied.", 500);

        var parent = new Parent
        {
            User = new User
            {
                RoleId = parentRole.Id,
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Phone = request.Phone?.Trim(),
                Address = request.Address?.Trim(),
                IsActive = true
            },
            Occupation = request.Occupation?.Trim()
        };

        _context.Parents.Add(parent);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(parent.Id, cancellationToken);
    }

    public async Task<ParentResponse> UpdateAsync(Guid id, UpdateParentRequest request, CancellationToken cancellationToken)
    {
        var parent = await _context.Parents.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Parent not found.", 404);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(x => x.Id != parent.UserId && x.Email == email, cancellationToken))
        {
            throw new AppException("A user with this email already exists.");
        }

        parent.Occupation = request.Occupation?.Trim();
        parent.User!.FullName = request.FullName.Trim();
        parent.User.Email = email;
        parent.User.Phone = request.Phone?.Trim();
        parent.User.Address = request.Address?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var parent = await _context.Parents.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Parent not found.", 404);

        if (parent.User is not null)
        {
            _context.Users.Remove(parent.User);
        }
        else
        {
            _context.Parents.Remove(parent);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Parent> BuildParentQuery()
    {
        return _context.Parents.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Students);
    }
}

public sealed class TeacherService : ITeacherService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public TeacherService(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResponse<TeacherResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        return await _context.Teachers.AsNoTracking()
            .Include(x => x.User)
            .OrderBy(x => x.User!.FullName)
            .ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<TeacherResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var teacher = await _context.Teachers.AsNoTracking().Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Teacher not found.", 404);

        return teacher.ToResponse();
    }

    public async Task<TeacherResponse> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var teacher = await _context.Teachers.AsNoTracking().Include(x => x.User).SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new AppException("Teacher profile not found.", 404);

        return teacher.ToResponse();
    }

    public async Task<TeacherResponse> CreateAsync(CreateTeacherRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new AppException("A user with this email already exists.");
        }

        if (await _context.Teachers.AnyAsync(x => x.TeacherCode == request.TeacherCode.Trim(), cancellationToken))
        {
            throw new AppException("Teacher code already exists.");
        }

        var teacherRole = await _context.Roles.SingleOrDefaultAsync(x => x.Name == "Teacher", cancellationToken)
            ?? throw new AppException("Teacher role is missing. Seed data has not been applied.", 500);

        var teacher = new Teacher
        {
            User = new User
            {
                RoleId = teacherRole.Id,
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Phone = request.Phone?.Trim(),
                Address = request.Address?.Trim(),
                IsActive = true
            },
            TeacherCode = request.TeacherCode.Trim(),
            Specialization = request.Specialization.Trim(),
            HireDate = request.HireDate
        };

        _context.Teachers.Add(teacher);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(teacher.Id, cancellationToken);
    }

    public async Task<TeacherResponse> UpdateAsync(Guid id, UpdateTeacherRequest request, CancellationToken cancellationToken)
    {
        var teacher = await _context.Teachers.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Teacher not found.", 404);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(x => x.Id != teacher.UserId && x.Email == email, cancellationToken))
        {
            throw new AppException("A user with this email already exists.");
        }

        if (await _context.Teachers.AnyAsync(x => x.Id != id && x.TeacherCode == request.TeacherCode.Trim(), cancellationToken))
        {
            throw new AppException("Teacher code already exists.");
        }

        teacher.TeacherCode = request.TeacherCode.Trim();
        teacher.Specialization = request.Specialization.Trim();
        teacher.HireDate = request.HireDate;
        teacher.User!.FullName = request.FullName.Trim();
        teacher.User.Email = email;
        teacher.User.Phone = request.Phone?.Trim();
        teacher.User.Address = request.Address?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var teacher = await _context.Teachers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Teacher not found.", 404);

        _context.Teachers.Remove(teacher);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ClassService : IClassService
{
    private readonly AppDbContext _context;
    private readonly TeacherScopeService _teacherScopeService;

    public ClassService(AppDbContext context, TeacherScopeService teacherScopeService)
    {
        _context = context;
        _teacherScopeService = teacherScopeService;
    }

    public async Task<PagedResponse<ClassResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        return await BuildClassQuery().OrderBy(x => x.Name).ThenBy(x => x.Section).ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<ClassResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var academicClass = await BuildClassQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Class not found.", 404);

        return academicClass.ToResponse();
    }

    public async Task<PagedResponse<ClassResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);

        return await _teacherScopeService.ApplyClassScope(BuildClassQuery(), teacherId)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Section)
            .ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<ClassResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        var academicClass = await _teacherScopeService.ApplyClassScope(BuildClassQuery(), teacherId)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Class not found.", 404);

        return academicClass.ToResponse();
    }

    public async Task<ClassResponse> CreateAsync(CreateClassRequest request, CancellationToken cancellationToken)
    {
        await EnsureTeacherExistsIfSuppliedAsync(request.ClassTeacherId, cancellationToken);

        var academicClass = new AcademicClass
        {
            Name = request.Name.Trim(),
            Section = request.Section.Trim(),
            AcademicYear = request.AcademicYear.Trim(),
            ClassTeacherId = request.ClassTeacherId
        };

        _context.AcademicClasses.Add(academicClass);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(academicClass.Id, cancellationToken);
    }

    public async Task<ClassResponse> UpdateAsync(Guid id, UpdateClassRequest request, CancellationToken cancellationToken)
    {
        var academicClass = await _context.AcademicClasses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Class not found.", 404);

        await EnsureTeacherExistsIfSuppliedAsync(request.ClassTeacherId, cancellationToken);

        academicClass.Name = request.Name.Trim();
        academicClass.Section = request.Section.Trim();
        academicClass.AcademicYear = request.AcademicYear.Trim();
        academicClass.ClassTeacherId = request.ClassTeacherId;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var academicClass = await _context.AcademicClasses.Include(x => x.Students).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Class not found.", 404);

        if (academicClass.Students.Count > 0)
        {
            throw new AppException("Class cannot be deleted while students are assigned to it.");
        }

        _context.AcademicClasses.Remove(academicClass);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AcademicClass> BuildClassQuery()
    {
        return _context.AcademicClasses.AsNoTracking()
            .Include(x => x.ClassTeacher).ThenInclude(x => x!.User)
            .Include(x => x.Students);
    }

    private async Task EnsureTeacherExistsIfSuppliedAsync(Guid? teacherId, CancellationToken cancellationToken)
    {
        if (teacherId.HasValue && !await _context.Teachers.AnyAsync(x => x.Id == teacherId.Value, cancellationToken))
        {
            throw new AppException("Teacher not found.", 404);
        }
    }
}

public sealed class SubjectService : ISubjectService
{
    private readonly AppDbContext _context;
    private readonly TeacherScopeService _teacherScopeService;

    public SubjectService(AppDbContext context, TeacherScopeService teacherScopeService)
    {
        _context = context;
        _teacherScopeService = teacherScopeService;
    }

    public async Task<PagedResponse<SubjectResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        return await _context.Subjects.AsNoTracking().OrderBy(x => x.Name).ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<SubjectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var subject = await _context.Subjects.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Subject not found.", 404);

        return subject.ToResponse();
    }

    public async Task<PagedResponse<SubjectResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);

        return await _teacherScopeService.ApplySubjectScope(_context.Subjects.AsNoTracking(), teacherId)
            .OrderBy(x => x.Name)
            .ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<SubjectResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken)
    {
        var teacherId = await _teacherScopeService.GetTeacherIdByUserIdAsync(teacherUserId, cancellationToken);
        var subject = await _teacherScopeService.ApplySubjectScope(_context.Subjects.AsNoTracking(), teacherId)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Subject not found.", 404);

        return subject.ToResponse();
    }

    public async Task<SubjectResponse> CreateAsync(CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        if (await _context.Subjects.AnyAsync(x => x.Code == code, cancellationToken))
        {
            throw new AppException("Subject code already exists.");
        }

        var subject = new Subject
        {
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description?.Trim()
        };

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync(cancellationToken);
        return subject.ToResponse();
    }

    public async Task<SubjectResponse> UpdateAsync(Guid id, UpdateSubjectRequest request, CancellationToken cancellationToken)
    {
        var subject = await _context.Subjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Subject not found.", 404);

        var code = request.Code.Trim();
        if (await _context.Subjects.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken))
        {
            throw new AppException("Subject code already exists.");
        }

        subject.Name = request.Name.Trim();
        subject.Code = code;
        subject.Description = request.Description?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return subject.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var subject = await _context.Subjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Subject not found.", 404);

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
