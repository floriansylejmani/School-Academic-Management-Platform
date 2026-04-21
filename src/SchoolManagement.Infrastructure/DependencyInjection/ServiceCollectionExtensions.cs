using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.AI;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Reports;
using SchoolManagement.Infrastructure.AI;
using SchoolManagement.Infrastructure.Authentication;
using SchoolManagement.Infrastructure.Reports;

namespace SchoolManagement.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<PasswordResetSettings>(configuration.GetSection(PasswordResetSettings.SectionName));
        services.Configure<OpenAISettings>(configuration.GetSection(OpenAISettings.SectionName));
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IReportPdfGenerator, QuestPdfReportGenerator>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IPasswordResetNotifier, LoggingPasswordResetNotifier>();
        services.AddHttpClient<IAIGradingService, OpenAIGradingService>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAISettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        return services;
    }
}
