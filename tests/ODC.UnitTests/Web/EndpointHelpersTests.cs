using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi;
using ODC.Web.Endpoints;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Web;

/// <summary>
/// The pieces every endpoint shares: results as HTTP answers (status codes and the <c>code</c> the client relies on),
/// the antiforgery check of state-changing requests, form file uploads and the enum schemas of the OpenAPI document.
/// </summary>
public sealed class EndpointHelpersTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.TooManyRequests, 429)]
    [InlineData(ErrorType.Unavailable, 503)]
    public void Every_error_type_has_its_status_code_and_carries_its_code_and_message(ErrorType type, int status)
    {
        var problem = new Error("signing_turn", "Документ зараз підписує інший користувач.", type).ToProblem();

        Assert.Equal(status, problem.StatusCode);
        Assert.Equal("signing_turn", problem.ProblemDetails.Title);
        Assert.Equal("Документ зараз підписує інший користувач.", problem.ProblemDetails.Detail);
        Assert.Equal("signing_turn", problem.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public void An_unknown_error_type_answers_400()
    {
        Assert.Equal(400, new Error("x", "y", (ErrorType)99).ToProblem().StatusCode);
    }

    [Fact]
    public void A_value_answers_200_with_the_value_and_a_failure_answers_the_problem()
    {
        var ok = Assert.IsType<Ok<string>>(Result<string>.Success("готово").ToOk().Result);
        Assert.Equal("готово", ok.Value);

        var problem = Assert.IsType<ProblemHttpResult>(Result<string>.Failure(Error.NotFound()).ToOk().Result);
        Assert.Equal(404, problem.StatusCode);
    }

    [Fact]
    public void A_created_record_answers_201_with_its_address_and_id()
    {
        var id = Guid.NewGuid();

        var created = Assert.IsType<Created<CreatedId>>(Result<Guid>.Success(id).ToCreated(static i => $"/api/v1/documents/{i}").Result);

        Assert.Equal($"/api/v1/documents/{id}", created.Location);
        Assert.Equal(id, created.Value!.Id);
        Assert.IsType<ProblemHttpResult>(Result<Guid>.Failure(Error.Forbidden()).ToCreated(static i => "/never").Result);
    }

    [Fact]
    public void A_completed_command_answers_204_and_a_failure_the_problem()
    {
        Assert.IsType<NoContent>(Result.Success().ToNoContent().Result);
        Assert.Equal(409, Assert.IsType<ProblemHttpResult>(Result.Failure(Error.Conflict("Зайнято.")).ToNoContent().Result).StatusCode);
    }

    [Fact]
    public void A_file_is_a_download_with_its_name_type_and_range_support()
    {
        var file = new DownloadableFile(new MemoryStream([1, 2]), "наказ.pdf", "application/pdf");

        var result = Assert.IsType<FileStreamHttpResult>(Result<DownloadableFile>.Success(file).ToFile().Result);

        Assert.Equal("наказ.pdf", result.FileDownloadName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.True(result.EnableRangeProcessing);
        Assert.IsType<ProblemHttpResult>(Result<DownloadableFile>.Failure(Error.NotFound()).ToFile().Result);
    }

    [Fact]
    public void Null_results_are_programming_errors()
    {
        Assert.Throws<ArgumentNullException>(() => ((Error)null!).ToProblem());
        Assert.Throws<ArgumentNullException>(() => ((Result<string>)null!).ToOk());
        Assert.Throws<ArgumentNullException>(() => ((Result)null!).ToNoContent());
        Assert.Throws<ArgumentNullException>(() => Result<Guid>.Success(Guid.Empty).ToCreated(null!));
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("TRACE")]
    public async Task Safe_methods_pass_without_a_token(string method)
    {
        var antiforgery = new ScriptedAntiforgery(valid: false);

        var result = await Filter(antiforgery, method);

        Assert.Equal("next", result);
        Assert.Equal(0, antiforgery.Checks);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task State_changing_methods_without_a_valid_token_are_refused_before_the_endpoint(string method)
    {
        var antiforgery = new ScriptedAntiforgery(valid: false);

        var problem = Assert.IsType<ProblemHttpResult>(await Filter(antiforgery, method));

        Assert.Equal(400, problem.StatusCode);
        Assert.Equal("antiforgery", problem.ProblemDetails.Title);
        Assert.Equal("Сесію сторінки оновлено. Оновіть сторінку та повторіть дію.", problem.ProblemDetails.Detail);
        Assert.Equal(1, antiforgery.Checks);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task State_changing_methods_with_a_valid_token_reach_the_endpoint(string method)
    {
        Assert.Equal("next", await Filter(new ScriptedAntiforgery(valid: true), method));
    }

    [Fact]
    public async Task Uploaded_form_files_are_passed_on_and_closed_afterwards()
    {
        var first = new TrackingStream([1, 2, 3]);
        var second = new TrackingStream([4]);
        IFormFile[] files = [Form(first, "a.pdf", "application/pdf"), Form(second, "b.png", "image/png")];

        var names = await FormFiles.WithUploadsAsync(files, static uploads => Task.FromResult(uploads.Select(static u => $"{u.FileName}:{u.ContentType}:{u.Length}:{u.Content.CanRead}").ToList()));

        Assert.Equal(["a.pdf:application/pdf:3:True", "b.png:image/png:1:True"], names);
        Assert.True(first.Closed);
        Assert.True(second.Closed);
    }

    [Fact]
    public async Task Form_files_are_closed_even_when_the_action_fails()
    {
        var stream = new TrackingStream([1]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => FormFiles.WithUploadsAsync<int>([Form(stream, "a.pdf", "application/pdf")], static _ => throw new InvalidOperationException()));

        Assert.True(stream.Closed);
    }

    [Fact]
    public async Task No_form_files_give_an_empty_list()
    {
        Assert.Equal(0, await FormFiles.WithUploadsAsync(null, static uploads => Task.FromResult(uploads.Count)));
    }

    [Fact]
    public async Task An_enum_schema_loses_its_null_value_and_null_type()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null, Enum = [JsonValue.Create("Draft"), null!, JsonValue.Create("Signed")] };

        await new NonNullableEnumSchemaTransformer().TransformAsync(schema, null!, TestContext.Current.CancellationToken);

        Assert.Equal(JsonSchemaType.String, schema.Type);
        Assert.Equal(["Draft", "Signed"], schema.Enum.Select(static v => v!.GetValue<string>()));
    }

    [Fact]
    public async Task Other_schemas_stay_as_they_are()
    {
        var nullableText = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null };
        var enumeration = new OpenApiSchema { Type = JsonSchemaType.String, Enum = [JsonValue.Create("A")] };

        await new NonNullableEnumSchemaTransformer().TransformAsync(nullableText, null!, TestContext.Current.CancellationToken);
        await new NonNullableEnumSchemaTransformer().TransformAsync(enumeration, null!, TestContext.Current.CancellationToken);

        Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, nullableText.Type);
        Assert.Single(enumeration.Enum);
    }

    /// <summary>
    /// Runs the antiforgery filter for a request with the given method.
    /// </summary>
    /// <param name="antiforgery">The antiforgery service.</param>
    /// <param name="method">The HTTP method.</param>
    /// <returns>What the filter returned (<c>"next"</c> when the endpoint ran).</returns>
    private static async Task<object?> Filter(IAntiforgery antiforgery, string method)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        return await new AntiforgeryEndpointFilter(antiforgery).InvokeAsync(new DefaultEndpointFilterInvocationContext(http), static _ => ValueTask.FromResult<object?>("next"));
    }

    /// <summary>
    /// A form file over a stream.
    /// </summary>
    /// <param name="stream">The content.</param>
    /// <param name="name">The file name.</param>
    /// <param name="type">The content type.</param>
    /// <returns>The form file.</returns>
    private static UploadedFile Form(Stream stream, string name, string type) => new(stream, name, type);

    /// <summary>
    /// An antiforgery service with a fixed answer.
    /// </summary>
    /// <param name="valid">Whether requests carry a valid token.</param>
    private sealed class ScriptedAntiforgery(bool valid) : IAntiforgery
    {
        /// <summary>
        /// Gets how many requests were checked.
        /// </summary>
        public int Checks { get; private set; }

        /// <inheritdoc />
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => throw new NotSupportedException();

        /// <inheritdoc />
        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => throw new NotSupportedException();

        /// <inheritdoc />
        public Task<bool> IsRequestValidAsync(HttpContext httpContext)
        {
            Checks++;
            return Task.FromResult(valid);
        }

        /// <inheritdoc />
        public void SetCookieTokenAndHeader(HttpContext httpContext) => throw new NotSupportedException();

        /// <inheritdoc />
        public Task ValidateRequestAsync(HttpContext httpContext) => throw new NotSupportedException();
    }

    /// <summary>
    /// A form file that hands out its own stream (so the test sees it closed).
    /// </summary>
    /// <param name="stream">The content.</param>
    /// <param name="name">The file name.</param>
    /// <param name="type">The content type.</param>
    private sealed class UploadedFile(Stream stream, string name, string type) : IFormFile
    {
        /// <inheritdoc />
        public string ContentType => type;

        /// <inheritdoc />
        public string ContentDisposition => $"form-data; name=\"files\"; filename=\"{name}\"";

        /// <inheritdoc />
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();

        /// <inheritdoc />
        public long Length => stream.Length;

        /// <inheritdoc />
        public string Name => "files";

        /// <inheritdoc />
        public string FileName => name;

        /// <inheritdoc />
        public void CopyTo(Stream target) => stream.CopyTo(target);

        /// <inheritdoc />
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => stream.CopyToAsync(target, cancellationToken);

        /// <inheritdoc />
        public Stream OpenReadStream() => stream;
    }

    /// <summary>
    /// A stream that remembers being closed.
    /// </summary>
    /// <param name="content">The content.</param>
    private sealed class TrackingStream(byte[] content) : MemoryStream(content)
    {
        /// <summary>
        /// Gets a value indicating whether the stream was closed.
        /// </summary>
        public bool Closed { get; private set; }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            Closed = true;
            base.Dispose(disposing);
        }
    }
}
