using FluentValidation;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Common.Validation;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Students;

public sealed record StudentResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    string StudentCode,
    DateOnly DateOfBirth,
    Gender Gender,
    DateOnly AdmissionDate,
    Guid? ParentId,
    string? ParentName,
    Guid? ClassId,
    string? ClassName,
    DateTime CreatedAt);

public sealed record CreateStudentRequest(
    string FullName,
    string Email,
    string Password,
    string? Phone,
    string? Address,
    string StudentCode,
    DateOnly DateOfBirth,
    Gender Gender,
    DateOnly AdmissionDate,
    Guid? ParentId,
    Guid? ClassId);

public sealed record UpdateStudentRequest(
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    string StudentCode,
    DateOnly DateOfBirth,
    Gender Gender,
    DateOnly AdmissionDate,
    Guid? ParentId,
    Guid? ClassId);

public interface IStudentService
{
    Task<PagedResponse<StudentResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<StudentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<StudentResponse> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<PagedResponse<StudentResponse>> GetByParentUserIdAsync(Guid parentUserId, PaginationRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<StudentResponse>> GetForTeacherUserAsync(Guid teacherUserId, PaginationRequest request, CancellationToken cancellationToken);
    Task<StudentResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken);
    Task<bool> CanTeacherAccessStudentAsync(Guid teacherUserId, Guid studentId, CancellationToken cancellationToken);
    Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken);
    Task<StudentResponse> UpdateAsync(Guid id, UpdateStudentRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
{
    public CreateStudentRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(150)
            .Must(StudentValidationHelpers.NotContainMarkup)
            .WithMessage("Full name must not contain markup.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.StudentCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DateOfBirth).ValidDateOfBirth();
        RuleFor(x => x.AdmissionDate).ValidAdmissionDate();
    }
}

public sealed class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(150)
            .Must(StudentValidationHelpers.NotContainMarkup)
            .WithMessage("Full name must not contain markup.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.StudentCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DateOfBirth).ValidDateOfBirth();
        RuleFor(x => x.AdmissionDate).ValidAdmissionDate();
    }

}

internal static class StudentValidationHelpers
{
    public static bool NotContainMarkup(string value) =>
        value.IndexOf('<') < 0 && value.IndexOf('>') < 0;
}
