using FluentValidation;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Fees;

public sealed record PaymentResponse(
    Guid Id,
    Guid FeeId,
    string FeeType,
    Guid StudentId,
    string StudentName,
    decimal AmountPaid,
    DateTime PaymentDate,
    PaymentMethod PaymentMethod,
    string? TransactionReference,
    string? IdempotencyKey);

public sealed record FeeResponse(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string FeeType,
    decimal Amount,
    DateOnly DueDate,
    string Status,
    IReadOnlyCollection<PaymentResponse> Payments,
    DateTime CreatedAt);

public sealed record CreateFeeRequest(Guid StudentId, string FeeType, decimal Amount, DateOnly DueDate, FeeStatus Status);

public sealed record UpdateFeeRequest(Guid StudentId, string FeeType, decimal Amount, DateOnly DueDate, FeeStatus Status);

public sealed record CreatePaymentRequest(
    Guid FeeId,
    decimal AmountPaid,
    DateTime PaymentDate,
    PaymentMethod PaymentMethod,
    string? TransactionReference,
    string? IdempotencyKey = null);

public sealed class FeeFilterRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public Guid? StudentId { get; init; }
    public FeeStatus? Status { get; init; }
    public DateOnly? DueDateFrom { get; init; }
    public DateOnly? DueDateTo { get; init; }
}

public sealed class PaymentFilterRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public Guid? StudentId { get; init; }
    public Guid? FeeId { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
}

public interface IFeeService
{
    Task<PagedResponse<FeeResponse>> GetPagedAsync(FeeFilterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FeeResponse>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken);
    Task<FeeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<FeeResponse> CreateAsync(CreateFeeRequest request, CancellationToken cancellationToken);
    Task<FeeResponse> UpdateAsync(Guid id, UpdateFeeRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<PaymentResponse>> GetPaymentsPagedAsync(PaymentFilterRequest request, CancellationToken cancellationToken);
    Task<PaymentResponse> AddPaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken);
    /// <summary>Scans for overdue fees and sends a reminder notification to each affected student and parent.</summary>
    Task<int> NotifyOverdueFeesAsync(CancellationToken cancellationToken);
}

public sealed class CreateFeeRequestValidator : AbstractValidator<CreateFeeRequest>
{
    public CreateFeeRequestValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.FeeType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DueDate).GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1));
    }
}

public sealed class UpdateFeeRequestValidator : AbstractValidator<UpdateFeeRequest>
{
    public UpdateFeeRequestValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.FeeType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DueDate).GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1));
    }
}

public sealed class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.FeeId).NotEmpty();
        RuleFor(x => x.AmountPaid).GreaterThan(0);
        RuleFor(x => x.PaymentDate).LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5));
        RuleFor(x => x.TransactionReference).MaximumLength(100);
        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9._:-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.IdempotencyKey))
            .WithMessage("IdempotencyKey may contain only letters, numbers, dots, underscores, colons, and hyphens.");
    }
}

public sealed class FeeFilterRequestValidator : AbstractValidator<FeeFilterRequest>
{
    public FeeFilterRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x)
            .Must(x => !x.DueDateFrom.HasValue || !x.DueDateTo.HasValue || x.DueDateFrom <= x.DueDateTo)
            .WithMessage("DueDateFrom cannot be later than DueDateTo.");
    }
}

public sealed class PaymentFilterRequestValidator : AbstractValidator<PaymentFilterRequest>
{
    public PaymentFilterRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom <= x.DateTo)
            .WithMessage("DateFrom cannot be later than DateTo.");
    }
}
