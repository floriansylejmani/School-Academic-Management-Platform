using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Fees;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Persistence.Common;

namespace SchoolManagement.Persistence.Services;

public sealed class FeeService : IFeeService
{
    private readonly AppDbContext _context;

    public FeeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<FeeResponse>> GetPagedAsync(FeeFilterRequest request, CancellationToken cancellationToken)
    {
        var query = BuildFeeQuery();

        if (request.StudentId.HasValue)
        {
            query = query.Where(x => x.StudentId == request.StudentId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.DueDateFrom.HasValue)
        {
            query = query.Where(x => x.DueDate >= request.DueDateFrom.Value);
        }

        if (request.DueDateTo.HasValue)
        {
            query = query.Where(x => x.DueDate <= request.DueDateTo.Value);
        }

        return await query
            .OrderByDescending(x => x.DueDate)
            .ToPagedResponseAsync(
                new PaginationRequest { PageNumber = request.PageNumber, PageSize = request.PageSize },
                cancellationToken,
                x => x.ToResponse());
    }

    public async Task<FeeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var fee = await BuildFeeQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Fee not found.", 404);

        return fee.ToResponse();
    }

    public async Task<IReadOnlyCollection<FeeResponse>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var fees = await BuildFeeQuery()
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.DueDate)
            .ToListAsync(cancellationToken);

        return fees.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<FeeResponse> CreateAsync(CreateFeeRequest request, CancellationToken cancellationToken)
    {
        await EnsureStudentExistsAsync(request.StudentId, cancellationToken);

        var fee = new Fee
        {
            StudentId = request.StudentId,
            FeeType = request.FeeType.Trim(),
            Amount = request.Amount,
            DueDate = request.DueDate,
            Status = request.Status
        };

        _context.Fees.Add(fee);

        var student = await _context.Students
            .Include(x => x.User)
            .Include(x => x.Parent).ThenInclude(x => x!.User)
            .SingleOrDefaultAsync(x => x.Id == request.StudentId, cancellationToken);

        if (student is not null)
        {
            var dueStr = request.DueDate.ToString("MMM d, yyyy");
            _context.Notifications.Add(new Notification
            {
                UserId = student.UserId,
                Title = "New fee assigned",
                Message = $"A new fee of {request.Amount:C} for '{request.FeeType.Trim()}' has been assigned, due {dueStr}."
            });

            if (student.Parent?.UserId is { } parentUserId)
            {
                var studentName = student.User?.FullName ?? "Your child";
                _context.Notifications.Add(new Notification
                {
                    UserId = parentUserId,
                    StudentId = student.Id,
                    Title = "New fee assigned",
                    Message = $"A fee of {request.Amount:C} for '{request.FeeType.Trim()}' has been assigned to {studentName}, due {dueStr}."
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(fee.Id, cancellationToken);
    }

    public async Task<FeeResponse> UpdateAsync(Guid id, UpdateFeeRequest request, CancellationToken cancellationToken)
    {
        var fee = await _context.Fees.Include(x => x.Payments).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Fee not found.", 404);

        await EnsureStudentExistsAsync(request.StudentId, cancellationToken);

        fee.StudentId = request.StudentId;
        fee.FeeType = request.FeeType.Trim();
        fee.Amount = request.Amount;
        fee.DueDate = request.DueDate;
        fee.Status = fee.Payments.Count == 0 ? request.Status : fee.CalculateFeeStatus();

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<PaymentResponse> AddPaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = NormalizeOptional(request.IdempotencyKey);
        var transactionReference = NormalizeOptional(request.TransactionReference);
        var paymentDate = NormalizeToUtc(request.PaymentDate);

        if (idempotencyKey is not null)
        {
            var existingPayment = await FindPaymentByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existingPayment is not null)
            {
                EnsureIdempotentPaymentMatches(existingPayment, request, paymentDate, transactionReference);
                return existingPayment.ToResponse();
            }
        }

        var fee = await _context.Fees.SingleOrDefaultAsync(x => x.Id == request.FeeId, cancellationToken)
            ?? throw new AppException("Fee not found.", 404);

        var currentPaid = await _context.Payments
            .Where(x => x.FeeId == fee.Id)
            .SumAsync(x => x.AmountPaid, cancellationToken);
        if (currentPaid + request.AmountPaid > fee.Amount)
        {
            throw new AppException("Payment exceeds the remaining fee balance.");
        }

        var payment = new Payment
        {
            FeeId = fee.Id,
            AmountPaid = request.AmountPaid,
            PaymentDate = paymentDate,
            PaymentMethod = request.PaymentMethod,
            TransactionReference = transactionReference,
            IdempotencyKey = idempotencyKey
        };

        _context.Payments.Add(payment);
        var newStatus = CalculateFeeStatus(fee, currentPaid + request.AmountPaid);
        fee.Status = newStatus;

        var student = await _context.Students
            .Include(x => x.User)
            .Include(x => x.Parent).ThenInclude(x => x!.User)
            .SingleOrDefaultAsync(x => x.Id == fee.StudentId, cancellationToken);

        if (student is not null)
        {
            var statusLabel = newStatus == FeeStatus.Paid ? "fully paid" : $"partially paid ({currentPaid + request.AmountPaid:C} of {fee.Amount:C})";
            _context.Notifications.Add(new Notification
            {
                UserId = student.UserId,
                Title = "Payment received",
                Message = $"A payment of {request.AmountPaid:C} was recorded for '{fee.FeeType}'. Fee is now {statusLabel}."
            });

            if (student.Parent?.UserId is { } parentUserId)
            {
                var studentName = student.User?.FullName ?? "Your child";
                _context.Notifications.Add(new Notification
                {
                    UserId = parentUserId,
                    StudentId = student.Id,
                    Title = "Payment received",
                    Message = $"A payment of {request.AmountPaid:C} was recorded for {studentName}'s '{fee.FeeType}' fee. Fee is now {statusLabel}."
                });
            }
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (idempotencyKey is not null)
        {
            DetachAddedEntities();
            var existingPayment = await FindPaymentByIdempotencyKeyAsync(idempotencyKey, cancellationToken)
                ?? throw new AppException("Payment could not be recorded because the idempotency key is already in use.", 409);

            EnsureIdempotentPaymentMatches(existingPayment, request, paymentDate, transactionReference);
            return existingPayment.ToResponse();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("foreign key", StringComparison.OrdinalIgnoreCase) == true
                                        || ex.InnerException?.Message.Contains("violates", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new AppException("The fee no longer exists and the payment could not be recorded.", 409);
        }

        return new PaymentResponse(
            payment.Id,
            fee.Id,
            fee.FeeType,
            fee.StudentId,
            student?.User?.FullName ?? string.Empty,
            payment.AmountPaid,
            payment.PaymentDate,
            payment.PaymentMethod,
            payment.TransactionReference,
            payment.IdempotencyKey);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var fee = await _context.Fees.Include(x => x.Payments).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Fee not found.", 404);

        if (fee.Payments.Count > 0)
        {
            throw new AppException("Fee cannot be deleted while payments are recorded against it.");
        }

        _context.Fees.Remove(fee);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResponse<PaymentResponse>> GetPaymentsPagedAsync(PaymentFilterRequest request, CancellationToken cancellationToken)
    {
        var query = BuildPaymentQuery();

        if (request.StudentId.HasValue)
        {
            query = query.Where(x => x.Fee != null && x.Fee.StudentId == request.StudentId.Value);
        }

        if (request.FeeId.HasValue)
        {
            query = query.Where(x => x.FeeId == request.FeeId.Value);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(x => x.PaymentDate >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(x => x.PaymentDate <= request.DateTo.Value);
        }

        return await query
            .OrderByDescending(x => x.PaymentDate)
            .ToPagedResponseAsync(
                new PaginationRequest { PageNumber = request.PageNumber, PageSize = request.PageSize },
                cancellationToken,
                x => x.ToResponse());
    }

    public async Task<int> NotifyOverdueFeesAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var overdueFees = await _context.Fees
            .Include(x => x.Student).ThenInclude(x => x!.User)
            .Include(x => x.Student).ThenInclude(x => x!.Parent).ThenInclude(x => x!.User)
            .Where(x => x.DueDate < today && x.Status != FeeStatus.Paid)
            .ToListAsync(cancellationToken);

        if (overdueFees.Count == 0)
        {
            return 0;
        }

        foreach (var fee in overdueFees)
        {
            fee.Status = FeeStatus.Overdue;
            var student = fee.Student;
            if (student is null) continue;

            var dueStr = fee.DueDate.ToString("MMM d, yyyy");
            _context.Notifications.Add(new Notification
            {
                UserId = student.UserId,
                Title = "Fee overdue",
                Message = $"Your fee of {fee.Amount:C} for '{fee.FeeType}' was due on {dueStr} and remains unpaid."
            });

            if (student.Parent?.UserId is { } parentUserId)
            {
                var studentName = student.User?.FullName ?? "Your child";
                _context.Notifications.Add(new Notification
                {
                    UserId = parentUserId,
                    StudentId = student.Id,
                    Title = "Fee overdue",
                    Message = $"{studentName}'s fee of {fee.Amount:C} for '{fee.FeeType}' was due on {dueStr} and remains unpaid."
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return overdueFees.Count;
    }

    private IQueryable<Fee> BuildFeeQuery()
    {
        return _context.Fees.AsNoTracking()
            .Include(x => x.Student).ThenInclude(x => x!.User)
            .Include(x => x.Payments);
    }

    private IQueryable<Payment> BuildPaymentQuery()
    {
        return _context.Payments.AsNoTracking()
            .Include(x => x.Fee).ThenInclude(x => x!.Student).ThenInclude(x => x!.User);
    }

    private async Task EnsureStudentExistsAsync(Guid studentId, CancellationToken cancellationToken)
    {
        if (!await _context.Students.AnyAsync(x => x.Id == studentId, cancellationToken))
        {
            throw new AppException("Student not found.", 404);
        }
    }

    private async Task<Payment?> FindPaymentByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        return await BuildPaymentQuery()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    private static void EnsureIdempotentPaymentMatches(
        Payment existingPayment,
        CreatePaymentRequest request,
        DateTime paymentDate,
        string? transactionReference)
    {
        if (existingPayment.FeeId != request.FeeId ||
            existingPayment.AmountPaid != request.AmountPaid ||
            existingPayment.PaymentDate != paymentDate ||
            existingPayment.PaymentMethod != request.PaymentMethod ||
            !string.Equals(existingPayment.TransactionReference, transactionReference, StringComparison.Ordinal))
        {
            throw new AppException("Idempotency key is already associated with a different payment request.", 409);
        }
    }

    private void DetachAddedEntities()
    {
        foreach (var entry in _context.ChangeTracker.Entries().Where(x => x.State == EntityState.Added))
        {
            entry.State = EntityState.Detached;
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };

        // PostgreSQL timestamps are stored at microsecond precision. Normalizing before
        // persistence keeps idempotency comparisons stable across providers.
        return new DateTime(utcValue.Ticks - (utcValue.Ticks % 10), DateTimeKind.Utc);
    }

    private static Domain.Enums.FeeStatus CalculateFeeStatus(Fee fee, decimal totalPaid)
    {
        if (totalPaid >= fee.Amount)
        {
            return Domain.Enums.FeeStatus.Paid;
        }

        if (totalPaid > 0)
        {
            return Domain.Enums.FeeStatus.PartiallyPaid;
        }

        return fee.DueDate < DateOnly.FromDateTime(DateTime.UtcNow)
            ? Domain.Enums.FeeStatus.Overdue
            : Domain.Enums.FeeStatus.Pending;
    }
}
