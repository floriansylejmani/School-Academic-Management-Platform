using System.Net;
using System.Net.Http.Json;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Fees;
using SchoolManagement.Application.Parents;
using SchoolManagement.Application.Students;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Common;

public static class ApiTestClientExtensions
{
    public static async Task<StudentResponse> CreateStudentAsync(this HttpClient client, CreateStudentRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/students", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadRequiredJsonAsync<ApiResponse<StudentResponse>>();
        return payload.Data ?? throw new InvalidOperationException("Student creation did not return data.");
    }

    public static async Task<ParentResponse> CreateParentAsync(this HttpClient client, CreateParentRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/parents", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadRequiredJsonAsync<ApiResponse<ParentResponse>>();
        return payload.Data ?? throw new InvalidOperationException("Parent creation did not return data.");
    }

    public static async Task<FeeResponse> CreateFeeAsync(this HttpClient client, CreateFeeRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/fees", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadRequiredJsonAsync<ApiResponse<FeeResponse>>();
        return payload.Data ?? throw new InvalidOperationException("Fee creation did not return data.");
    }

    public static async Task<PaymentResponse> AddPaymentAsync(this HttpClient client, CreatePaymentRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/payments", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadRequiredJsonAsync<ApiResponse<PaymentResponse>>();
        return payload.Data ?? throw new InvalidOperationException("Payment creation did not return data.");
    }

    public static async Task<ApiResponse<T>> ReadApiResponseAsync<T>(this HttpResponseMessage response)
    {
        return await response.ReadRequiredJsonAsync<ApiResponse<T>>();
    }

    public static async Task AssertStatusCodeAsync(this HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        if (response.StatusCode == expectedStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new Xunit.Sdk.XunitException(
            $"Expected status code {(int)expectedStatusCode} but got {(int)response.StatusCode}. Body: {body}");
    }
}
