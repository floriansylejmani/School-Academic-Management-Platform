using System.Net.Http.Json;
using System.Text.Json;

namespace SchoolManagement.Tests.Infrastructure;

public static class HttpResponseMessageExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<T> ReadRequiredJsonAsync<T>(this HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return payload ?? throw new InvalidOperationException($"Expected JSON payload of type {typeof(T).Name}.");
    }
}
