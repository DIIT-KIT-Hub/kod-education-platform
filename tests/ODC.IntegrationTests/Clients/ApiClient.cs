using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ODC.Web.Features.Auth;

namespace ODC.IntegrationTests.Clients;

/// <summary>
/// A browser-like client of the cabinet API: keeps the cookies (JWT, two-factor step, activation, antiforgery) and sends
/// the antiforgery token in <c>X-CSRF-TOKEN</c> with every state-changing request, as the React client does.
/// </summary>
/// <param name="http">The HTTP client of the test server (cookies on).</param>
public sealed class ApiClient(HttpClient http) : IDisposable
{
    /// <summary>
    /// The antiforgery token for the current identity.
    /// </summary>
    private string? _antiforgeryToken;

    /// <summary>
    /// Gets the JSON settings of the API (web defaults, enums as names).
    /// </summary>
    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    /// <summary>
    /// Gets the underlying HTTP client.
    /// </summary>
    public HttpClient Http => http;

    /// <summary>
    /// Gets the auth context of the last <see cref="RefreshContextAsync"/>.
    /// </summary>
    public AuthContextResponse? Context { get; private set; }

    /// <summary>
    /// Checks the status of a response and reads its JSON body.
    /// </summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="response">The response.</param>
    /// <returns>The body.</returns>
    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await ExpectAsync(response, HttpStatusCode.OK, HttpStatusCode.Created);
        var value = await response.Content.ReadFromJsonAsync<T>(Json, TestContext.Current.CancellationToken);
        return value ?? throw new InvalidOperationException($"{response.RequestMessage?.RequestUri} answered an empty body.");
    }

    /// <summary>
    /// Fails the test with the problem details of the response when its status is not one of <paramref name="expected"/>.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="expected">The expected statuses.</param>
    /// <returns>A task.</returns>
    public static async Task ExpectAsync(HttpResponseMessage response, params HttpStatusCode[] expected)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (!expected.Contains(response.StatusCode))
        {
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Fail($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} answered {(int)response.StatusCode}, expected {string.Join(" or ", expected.Select(static s => (int)s))}: {body}");
        }
    }

    /// <summary>
    /// Reads the <c>code</c> of a problem details answer.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <returns>The code, or null.</returns>
    public static async Task<string?> ProblemCodeAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    /// <summary>
    /// Loads the auth context: the signed-in user (if any) and a fresh antiforgery token. Call it after every change of
    /// the identity (sign-in steps, photo, sign-out), because the token is bound to the identity.
    /// </summary>
    /// <returns>The context.</returns>
    public async Task<AuthContextResponse> RefreshContextAsync()
    {
        Context = await GetAsync<AuthContextResponse>("/api/v1/auth/context");
        _antiforgeryToken = Context.AntiforgeryToken;
        return Context;
    }

    /// <summary>
    /// Sends a request; state-changing ones carry the antiforgery token.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="path">The path and query.</param>
    /// <param name="content">The body.</param>
    /// <param name="antiforgery">Whether to send the antiforgery token (false to test its absence).</param>
    /// <returns>The response.</returns>
    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content = null, bool antiforgery = true)
    {
        ArgumentNullException.ThrowIfNull(method);
        using var request = new HttpRequestMessage(method, path) { Content = content };
        if (antiforgery && method != HttpMethod.Get && method != HttpMethod.Head)
        {
            if (_antiforgeryToken is null)
            {
                await RefreshContextAsync();
            }

            request.Headers.Add("X-CSRF-TOKEN", _antiforgeryToken);
        }

        return await http.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Sends a GET request.
    /// </summary>
    /// <param name="path">The path and query.</param>
    /// <returns>The response.</returns>
    public Task<HttpResponseMessage> GetAsync(string path) => SendAsync(HttpMethod.Get, path);

    /// <summary>
    /// Sends a GET request and reads a successful JSON answer.
    /// </summary>
    /// <typeparam name="T">The answer type.</typeparam>
    /// <param name="path">The path and query.</param>
    /// <returns>The answer.</returns>
    public async Task<T> GetAsync<T>(string path)
    {
        using var response = await GetAsync(path);
        return await ReadAsync<T>(response);
    }

    /// <summary>
    /// Sends a JSON body.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="path">The path.</param>
    /// <param name="body">The body.</param>
    /// <param name="antiforgery">Whether to send the antiforgery token.</param>
    /// <returns>The response.</returns>
    public Task<HttpResponseMessage> SendJsonAsync(HttpMethod method, string path, object? body, bool antiforgery = true)
        => SendAsync(method, path, JsonContent.Create(body, options: Json), antiforgery);

    /// <summary>
    /// Posts a JSON body.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="body">The body.</param>
    /// <returns>The response.</returns>
    public Task<HttpResponseMessage> PostAsync(string path, object? body) => SendJsonAsync(HttpMethod.Post, path, body);

    /// <summary>
    /// Posts a JSON body and reads a successful JSON answer.
    /// </summary>
    /// <typeparam name="T">The answer type.</typeparam>
    /// <param name="path">The path.</param>
    /// <param name="body">The body.</param>
    /// <returns>The answer.</returns>
    public async Task<T> PostAsync<T>(string path, object? body)
    {
        using var response = await PostAsync(path, body);
        return await ReadAsync<T>(response);
    }

    /// <summary>
    /// Puts a JSON body.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="body">The body.</param>
    /// <returns>The response.</returns>
    public Task<HttpResponseMessage> PutAsync(string path, object? body) => SendJsonAsync(HttpMethod.Put, path, body);

    /// <summary>
    /// Sends a multipart form, as the client does for uploads.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="path">The path.</param>
    /// <param name="fields">The text fields (a name may repeat, for lists).</param>
    /// <param name="files">The files.</param>
    /// <returns>The response.</returns>
    public async Task<HttpResponseMessage> SendFormAsync(HttpMethod method, string path, IEnumerable<(string Name, string Value)> fields, params UploadedFile[] files)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(files);
        using var form = new MultipartFormDataContent();
        foreach (var (name, value) in fields)
        {
            form.Add(new StringContent(value), name);
        }

        foreach (var file in files)
        {
            form.Add(TestFiles.Part(file.Content, file.MediaType), file.Field, file.FileName);
        }

        return await SendAsync(method, path, form);
    }

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The response.</returns>
    public Task<HttpResponseMessage> DeleteAsync(string path) => SendAsync(HttpMethod.Delete, path);

    /// <inheritdoc />
    public void Dispose() => http.Dispose();
}
