using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FixtureHub.IntegrationTests.Api;

internal static class HttpClientExtensions
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    public static Task<HttpResponseMessage> PostJsonAsync(
        this HttpClient client,
        string url,
        object? body = null,
        string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = ToContent(body) };
        request.Headers.Add(IdempotencyKeyHeader, idempotencyKey ?? Guid.NewGuid().ToString());

        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> PostJsonWithoutKeyAsync(this HttpClient client, string url, object? body = null) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Post, url) { Content = ToContent(body) });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string username, string password) =>
        client.PostJsonWithoutKeyAsync("/api/auth/login", new { username, password });

    public static async Task<Guid> CreateAsync(this HttpClient client, string url, object body)
    {
        var response = await client.PostJsonAsync(url, body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.IdAsync();
    }

    public static async Task<Guid> IdAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

    public static async Task<JsonElement> ReadProblemAsync(this HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static string? ErrorCode(this JsonElement problem) =>
        problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;

    public static JsonElement Metadata(this JsonElement problem) => problem.GetProperty("metadata");

    private static JsonContent? ToContent(object? body) => body is null ? null : JsonContent.Create(body);
}
