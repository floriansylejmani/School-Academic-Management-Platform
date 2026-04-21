using FluentValidation;
using FluentValidation.Validators;
using SchoolManagement.Application.Common.Validation;

namespace SchoolManagement.Application.Common.Validation;

public static class CommonValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return (IRuleBuilderOptions<T, string>)ruleBuilder
            .NotEmpty()
            .MaximumLength(150)
            .EmailAddress()
            .Custom((email, context) => {
                if (!string.IsNullOrEmpty(email))
                {
                    var sanitized = InputSanitizer.SanitizeEmail(email);
                    if (string.IsNullOrEmpty(sanitized))
                    {
                        context.AddFailure("A valid email address is required.");
                    }
                }
            });
    }

    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> ruleBuilder, int maxLength = 150)
    {
        return (IRuleBuilderOptions<T, string>)ruleBuilder
            .NotEmpty()
            .MaximumLength(maxLength)
            .MinimumLength(2)
            .Custom((name, context) => {
                if (!string.IsNullOrEmpty(name))
                {
                    var sanitized = InputSanitizer.SanitizeText(name);
                    if (string.IsNullOrEmpty(sanitized) || !System.Text.RegularExpressions.Regex.IsMatch(sanitized, @"^[\p{L}\p{M}\s\-'\.]+$"))
                    {
                        context.AddFailure("Name must contain only letters (including international characters), spaces, hyphens, apostrophes, and periods.");
                    }
                }
            });
    }

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Must(password => HasValidPasswordStructure(password))
            .WithMessage("Password must be at least 8 characters long and contain at least one lowercase letter, one uppercase letter, one digit, and one special character.");
    }

    private static bool HasValidPasswordStructure(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        bool hasLower = password.Any(char.IsLower);
        bool hasUpper = password.Any(char.IsUpper);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        return hasLower && hasUpper && hasDigit && hasSpecial;
    }

    public static IRuleBuilderOptions<T, string> ValidPhone<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return (IRuleBuilderOptions<T, string>)ruleBuilder
            .MaximumLength(30)
            .Custom((phone, context) => {
                if (!string.IsNullOrEmpty(phone))
                {
                    var sanitized = InputSanitizer.SanitizePhoneNumber(phone);
                    if (!IsValidPhoneNumber(sanitized))
                    {
                        context.AddFailure("Phone number must be a valid international or local format.");
                    }
                }
            });
    }

    private static bool IsValidPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return true; // Empty is handled by When() condition

        // Allow international format (+country code) or local format
        // Remove all non-digit characters for validation
        var digitsOnly = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", string.Empty);
        
        // Valid phone numbers have 7-15 digits (including country code)
        return digitsOnly.Length >= 7 && digitsOnly.Length <= 15;
    }

    public static IRuleBuilderOptions<T, string> ValidAddress<T>(this IRuleBuilder<T, string> ruleBuilder, int maxLength = 500)
    {
        return (IRuleBuilderOptions<T, string>)ruleBuilder
            .MaximumLength(maxLength)
            .Custom((address, context) => {
                if (!string.IsNullOrEmpty(address))
                {
                    var sanitized = InputSanitizer.SanitizeText(address);
                    if (string.IsNullOrEmpty(sanitized))
                    {
                        context.AddFailure("Address contains invalid characters.");
                    }
                }
            });
    }

    public static IRuleBuilderOptions<T, string> ValidStudentCode<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return (IRuleBuilderOptions<T, string>)ruleBuilder
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[A-Za-z0-9\-]+$")
            .Custom((code, context) => {
                if (!string.IsNullOrEmpty(code))
                {
                    var sanitized = InputSanitizer.SanitizeText(code).ToUpperInvariant();
                    if (string.IsNullOrEmpty(sanitized) || !System.Text.RegularExpressions.Regex.IsMatch(sanitized, @"^[A-Z0-9\-]+$"))
                    {
                        context.AddFailure("Student code must contain only letters, numbers, and hyphens.");
                    }
                }
            });
    }

    public static IRuleBuilderOptions<T, string> ValidTitle<T>(this IRuleBuilder<T, string> ruleBuilder, int maxLength = 100)
    {
        return (IRuleBuilderOptions<T, string>)ruleBuilder
            .NotEmpty()
            .MaximumLength(maxLength)
            .MinimumLength(2)
            .Custom((title, context) => {
                if (!string.IsNullOrEmpty(title))
                {
                    var sanitized = InputSanitizer.SanitizeText(title);
                    if (string.IsNullOrEmpty(sanitized) || sanitized.Length < 2)
                    {
                        context.AddFailure("Title is required and must be valid.");
                    }
                }
            });
    }

    public static IRuleBuilderOptions<T, string> ValidDescription<T>(this IRuleBuilder<T, string> ruleBuilder, int maxLength = 1000)
    {
        return (IRuleBuilderOptions<T, string>)ruleBuilder
            .MaximumLength(maxLength)
            .Custom((desc, context) => {
                if (!string.IsNullOrEmpty(desc))
                {
                    var sanitized = InputSanitizer.SanitizeText(desc);
                    if (string.IsNullOrEmpty(sanitized))
                    {
                        context.AddFailure("Description contains invalid characters.");
                    }
                }
            });
    }

    public static IRuleBuilderOptions<T, string> ValidAcademicYear<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(20)
            .Matches(@"^\d{4}-\d{4}$")
            .WithMessage("Academic year must be in format YYYY-YYYY (e.g., 2023-2024).");
    }

    public static IRuleBuilderOptions<T, string> ValidSection<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return (IRuleBuilderOptions<T, string>)ruleBuilder
            .NotEmpty()
            .MaximumLength(20)
            .Matches(@"^[A-Za-z0-9]+$")
            .Custom((section, context) => {
                if (!string.IsNullOrEmpty(section))
                {
                    var sanitized = InputSanitizer.SanitizeText(section).ToUpperInvariant();
                    if (string.IsNullOrEmpty(sanitized) || !System.Text.RegularExpressions.Regex.IsMatch(sanitized, @"^[A-Z0-9]+$"))
                    {
                        context.AddFailure("Section must contain only letters and numbers.");
                    }
                }
            });
    }

    public static IRuleBuilderOptions<T, decimal> ValidMarks<T>(this IRuleBuilder<T, decimal> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .WithMessage("Marks must be between 0 and 1000.");
    }

    public static IRuleBuilderOptions<T, DateOnly> ValidDateOfBirth<T>(this IRuleBuilder<T, DateOnly> ruleBuilder)
    {
        return ruleBuilder
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-120)))
            .WithMessage("Date of birth must be a valid date in the past.");
    }

    public static IRuleBuilderOptions<T, DateOnly> ValidAdmissionDate<T>(this IRuleBuilder<T, DateOnly> ruleBuilder)
    {
        return ruleBuilder
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-50)))
            .WithMessage("Admission date must be a valid date not in the future.");
    }

    public static IRuleBuilderOptions<T, DateOnly> ValidExamDate<T>(this IRuleBuilder<T, DateOnly> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)))
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)))
            .WithMessage("Exam date must be within a reasonable range.");
    }
}
