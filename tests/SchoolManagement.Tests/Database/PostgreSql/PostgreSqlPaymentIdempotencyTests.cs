using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Fees;
using SchoolManagement.Application.Students;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Persistence;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Database.PostgreSql;

[Collection(PostgreSqlTestCollection.Name)]
[Trait("Category", "PostgreSQL")]
public sealed class PostgreSqlPaymentIdempotencyTests
{
    private readonly PostgreSqlSchoolManagementApiFactory _factory;

    public PostgreSqlPaymentIdempotencyTests(PostgreSqlSchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Same_idempotency_key_does_not_duplicate_payment()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var fee = await CreateFeeAsync(adminClient, "postgres.idem.same@school.com", "PG-IDEM-001", 200m);
        var request = new CreatePaymentRequest(fee.Id, 80m, DateTime.UtcNow, PaymentMethod.Card, "PG-TXN-001", "pg-idem-same");

        var first = await adminClient.AddPaymentAsync(request);
        var second = await adminClient.AddPaymentAsync(request);

        Assert.Equal(first.Id, second.Id);

        using var scope = _factory.CreateDbContextScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payments = await dbContext.Payments.Where(x => x.IdempotencyKey == "pg-idem-same").ToListAsync();
        var payment = Assert.Single(payments);
        Assert.Equal(80m, payment.AmountPaid);

        var reloadedFee = await dbContext.Fees.Include(x => x.Payments).SingleAsync(x => x.Id == fee.Id);
        Assert.Equal(FeeStatus.PartiallyPaid, reloadedFee.Status);
        Assert.Equal(80m, reloadedFee.Payments.Sum(x => x.AmountPaid));
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_amount_returns_conflict()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var fee = await CreateFeeAsync(adminClient, "postgres.idem.conflict@school.com", "PG-IDEM-002", 200m);
        var paymentDate = DateTime.UtcNow;

        await adminClient.AddPaymentAsync(
            new CreatePaymentRequest(fee.Id, 80m, paymentDate, PaymentMethod.Card, "PG-TXN-002", "pg-idem-conflict"));
        var conflict = await adminClient.PostAsJsonAsync(
            "/api/payments",
            new CreatePaymentRequest(fee.Id, 90m, paymentDate, PaymentMethod.Card, "PG-TXN-002", "pg-idem-conflict"));

        await conflict.AssertStatusCodeAsync(HttpStatusCode.Conflict);
        var payload = await conflict.ReadApiResponseAsync<object>();
        Assert.False(payload.Success);
        Assert.Equal("Idempotency key is already associated with a different payment request.", payload.Message);

        using var scope = _factory.CreateDbContextScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await dbContext.Payments.CountAsync(x => x.IdempotencyKey == "pg-idem-conflict"));
    }

    [Fact]
    public async Task Concurrent_requests_with_same_idempotency_key_do_not_create_duplicate_payment_rows()
    {
        await _factory.ResetDatabaseAsync();
        using var firstClient = await _factory.CreateAuthenticatedClientAsync();
        using var secondClient = await _factory.CreateAuthenticatedClientAsync();
        var fee = await CreateFeeAsync(firstClient, "postgres.idem.concurrent@school.com", "PG-IDEM-003", 200m);
        var request = new CreatePaymentRequest(fee.Id, 75m, DateTime.UtcNow, PaymentMethod.Online, "PG-TXN-003", "pg-idem-concurrent");

        var responses = await Task.WhenAll(
            firstClient.PostAsJsonAsync("/api/payments", request),
            secondClient.PostAsJsonAsync("/api/payments", request));

        Assert.All(responses, response =>
            Assert.True(
                response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
                $"Unexpected status code: {(int)response.StatusCode}"));

        using var scope = _factory.CreateDbContextScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payments = await dbContext.Payments.Where(x => x.IdempotencyKey == "pg-idem-concurrent").ToListAsync();
        var payment = Assert.Single(payments);
        Assert.Equal(75m, payment.AmountPaid);

        var reloadedFee = await dbContext.Fees.Include(x => x.Payments).SingleAsync(x => x.Id == fee.Id);
        Assert.Equal(75m, reloadedFee.Payments.Sum(x => x.AmountPaid));
    }

    private static async Task<FeeResponse> CreateFeeAsync(HttpClient adminClient, string studentEmail, string studentCode, decimal amount)
    {
        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest(
                "PostgreSQL Student",
                studentEmail,
                "Student@123",
                null,
                null,
                studentCode,
                new DateOnly(2011, 2, 2),
                Gender.Other,
                new DateOnly(2024, 9, 1),
                null,
                null));

        return await adminClient.CreateFeeAsync(
            new CreateFeeRequest(
                student.Id,
                "PostgreSQL Fee",
                amount,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                FeeStatus.Pending));
    }
}
