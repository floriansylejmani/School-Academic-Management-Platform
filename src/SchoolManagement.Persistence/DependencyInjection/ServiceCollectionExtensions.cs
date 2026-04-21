using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Analytics;
using SchoolManagement.Application.Files;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Exams;
using SchoolManagement.Application.Fees;
using SchoolManagement.Application.Notifications;
using SchoolManagement.Application.Parents;
using SchoolManagement.Application.Reports;
using SchoolManagement.Application.Results;
using SchoolManagement.Application.Roles;
using SchoolManagement.Application.Students;
using SchoolManagement.Application.Submissions;
using SchoolManagement.Application.Subjects;
using SchoolManagement.Application.Teachers;
using SchoolManagement.Application.Timetable;
using SchoolManagement.Application.Users;
using SchoolManagement.Persistence.Services;

namespace SchoolManagement.Persistence.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<TeacherScopeService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IParentService, ParentService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IResultService, ResultService>();
        services.AddScoped<IFeeService, FeeService>();
        services.AddScoped<ITimetableService, TimetableService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<NotificationRealtimeDispatcher>();

        return services;
    }
}
