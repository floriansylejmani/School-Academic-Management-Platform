using Microsoft.Extensions.Configuration;
using SchoolManagement.API.Common;
using SchoolManagement.Infrastructure.Authentication;

namespace SchoolManagement.Tests.Infrastructure;

public sealed class StartupConfigurationValidatorTests
{
    [Theory]
    [InlineData(JwtSettings.DefaultDevelopmentSecretKey)]
    [InlineData("ChangeMeForDemoOnly_AtLeast32Characters!")]
    public void Production_WithDemoJwtSecret_FailsConfigurationValidation(string secretKey)
    {
        var settings = ValidJwtSettings(secretKey);

        var exception = Assert.Throws<InvalidOperationException>(
            () => StartupConfigurationValidator.ValidateJwtSettings(settings, isDevelopment: false));

        Assert.Equal("Jwt:SecretKey must be changed for non-development environments.", exception.Message);
    }

    [Fact]
    public void Production_WithSeedDemoDataEnabled_FailsConfigurationValidation()
    {
        var configuration = BuildConfiguration(("Database:SeedDemoData", "true"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => StartupConfigurationValidator.ValidateDatabaseInitialization(configuration, isProduction: true));

        Assert.Equal("Database:SeedDemoData must be false in Production.", exception.Message);
    }

    [Fact]
    public void Production_WithAutoMigrateEnabled_FailsConfigurationValidation()
    {
        var configuration = BuildConfiguration(
            ("Database:AutoMigrate", "true"),
            ("Database:AllowProductionAutoMigrate", "false"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => StartupConfigurationValidator.ValidateDatabaseInitialization(configuration, isProduction: true));

        Assert.Equal(
            "Database:AutoMigrate must be false in Production unless Database:AllowProductionAutoMigrate is explicitly set to true.",
            exception.Message);
    }

    [Fact]
    public void Production_WithExplicitAutoMigrateOverride_PassesConfigurationValidation()
    {
        var configuration = BuildConfiguration(
            ("Database:AutoMigrate", "true"),
            ("Database:AllowProductionAutoMigrate", "true"),
            ("Database:SeedDemoData", "false"));

        StartupConfigurationValidator.ValidateDatabaseInitialization(configuration, isProduction: true);
    }

    [Fact]
    public void Development_WithDemoSettings_PassesConfigurationValidation()
    {
        var settings = ValidJwtSettings("ChangeMeForDemoOnly_AtLeast32Characters!");
        var configuration = BuildConfiguration(
            ("Database:AutoMigrate", "true"),
            ("Database:SeedDemoData", "true"));

        StartupConfigurationValidator.ValidateJwtSettings(settings, isDevelopment: true);
        StartupConfigurationValidator.ValidateDatabaseInitialization(configuration, isProduction: false);
    }

    private static JwtSettings ValidJwtSettings(string secretKey) => new()
    {
        Issuer = "SchoolManagement.Tests",
        Audience = "SchoolManagement.Tests.Client",
        SecretKey = secretKey,
        AccessTokenExpiryMinutes = 60,
        RefreshTokenExpiryDays = 7
    };

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(x => x.Key, x => (string?)x.Value))
            .Build();
    }
}
