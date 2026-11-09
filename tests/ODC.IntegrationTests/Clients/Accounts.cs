using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using ODC.IntegrationTests.Hosting;
using ODC.Web.Features.Auth;
using ODC.Web.Features.Auth.Activation;
using ODC.Web.Infrastructure.Identity;

namespace ODC.IntegrationTests.Clients;

/// <summary>
/// Signs people in through the real endpoints, the way the client does: password, then the authenticator (enrolled at the
/// first sign-in, as the cabinet requires), then the mandatory profile photo. Remembers every authenticator key and the
/// last time step used, and moves the application clock to the next step instead of reusing a code (codes are single-use).
/// </summary>
/// <param name="app">The application.</param>
public sealed class Accounts(OdcApplication app)
{
    /// <summary>
    /// The password the tests give to the activated administrator.
    /// </summary>
    public const string AdminPassword = "Admin-Integration-2026";

    /// <summary>
    /// The authenticator of every account that has one: the key and the last time step used.
    /// </summary>
    private readonly ConcurrentDictionary<string, Authenticator> _authenticators = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the activation of the administrator: once per run, the first time a test needs the account.
    /// </summary>
    private Lazy<Task> AdministratorActivation => field ??= new(ActivateAdministratorAsync, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Uploads a profile photo when the account has none (the API answers 403 until it has one).
    /// </summary>
    /// <param name="client">The signed-in client.</param>
    /// <returns>A task.</returns>
    public static async Task EnsurePhotoAsync(ApiClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        var context = client.Context ?? await client.RefreshContextAsync();
        if (context.User is null || context.User.PhotoId is not null)
        {
            return;
        }

        using var form = new MultipartFormDataContent { { TestFiles.Part(TestFiles.Png(), "image/png"), "file", "photo.png" } };
        await ApiClient.ExpectAsync(await client.SendAsync(HttpMethod.Put, "/api/v1/profile/photo", form), HttpStatusCode.OK);
        await client.RefreshContextAsync();
    }

    /// <summary>
    /// Signs a demo account in with the demo password.
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <returns>A signed-in client with a photo and a fresh antiforgery token.</returns>
    public Task<ApiClient> SignInAsync(string email) => SignInAsync(email, app.DemoPassword);

    /// <summary>
    /// Signs an account in.
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <param name="password">The password.</param>
    /// <returns>A signed-in client with a photo and a fresh antiforgery token.</returns>
    public async Task<ApiClient> SignInAsync(string email, string password)
    {
        var client = app.CreateApiClient();
        var login = await client.PostAsync<LoginResponse>("/api/v1/auth/login", new LoginRequest(email, password, RememberMe: false));
        await CompleteSecondFactorAsync(client, email, login.Next);
        await EnsurePhotoAsync(client);
        return client;
    }

    /// <summary>
    /// Signs the system administrator in, activating the account by e-mail the first time (it is created without a password).
    /// </summary>
    /// <returns>A signed-in client.</returns>
    public async Task<ApiClient> AdminAsync()
    {
        await AdministratorActivation.Value;
        return await SignInAsync(OdcApplication.AdminEmail, AdminPassword);
    }

    /// <summary>
    /// Activates an account that waits for its first password: the e-mailed code, the password, the authenticator and the
    /// photo, as the activation page does.
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <param name="password">The password to choose.</param>
    /// <param name="withPhoto">Whether to upload the profile photo too (the last step of the activation page).</param>
    /// <returns>A signed-in client.</returns>
    public async Task<ApiClient> ActivateAsync(string email, string password, bool withPhoto = true)
    {
        var client = app.CreateApiClient();
        await ApiClient.ExpectAsync(await client.PostAsync("/api/v1/auth/activation", new ActivationStartRequest(email)), HttpStatusCode.OK, HttpStatusCode.NoContent);
        var code = await app.Mailbox.WaitForCodeAsync(email);
        await client.PostAsync<ActivationVerifiedResponse>("/api/v1/auth/activation/code", new ActivationCodeRequest(code));
        var login = await client.PostAsync<LoginResponse>("/api/v1/auth/activation/password", new NewPasswordRequest(password));
        await CompleteSecondFactorAsync(client, email, login.Next);
        if (withPhoto)
        {
            await EnsurePhotoAsync(client);
        }

        return client;
    }

    /// <summary>
    /// Finishes a sign-in after the password: enrolls an authenticator ("setup") or enters its next code ("two-factor").
    /// </summary>
    /// <param name="client">The client in the middle of signing in.</param>
    /// <param name="email">The e-mail.</param>
    /// <param name="next">The step the server asked for.</param>
    /// <returns>A task.</returns>
    public async Task CompleteSecondFactorAsync(ApiClient client, string email, string next)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (next == LoginResponse.Setup)
        {
            var setup = await client.GetAsync<AuthenticatorSetupResponse>("/api/v1/auth/login/authenticator");
            _authenticators[email] = new Authenticator(setup.SharedKey);
            await client.PostAsync<RecoveryCodesResponse>("/api/v1/auth/login/authenticator", new CodeRequest(NextCode(email), RememberMe: false));
        }
        else
        {
            Assert.Equal(LoginResponse.TwoFactor, next);
            await ApiClient.ExpectAsync(await client.PostAsync("/api/v1/auth/login/two-factor", new CodeRequest(NextCode(email), RememberMe: false)), HttpStatusCode.NoContent);
        }

        await client.RefreshContextAsync();
    }

    /// <summary>
    /// The authenticator code for the next unused time step of an account, moving the application clock forward when the
    /// current step was already used.
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <returns>The six-digit code.</returns>
    public string NextCode(string email)
    {
        var authenticator = _authenticators.TryGetValue(email, out var known)
            ? known
            : throw new InvalidOperationException($"{email} has no authenticator yet.");
        var step = Totp.TimeStep(app.Clock.GetUtcNow());
        if (step <= authenticator.LastStep)
        {
            app.Clock.Advance(Totp.Step * (authenticator.LastStep - step + 1));
            step = Totp.TimeStep(app.Clock.GetUtcNow());
        }

        authenticator.LastStep = step;
        return Totp.Compute(Totp.DecodeBase32(authenticator.Key)!, step).ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The authenticator code of a time step that was already used (to test replay protection).
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <returns>The code of the last step used.</returns>
    public string LastCode(string email)
    {
        var authenticator = _authenticators[email];
        return Totp.Compute(Totp.DecodeBase32(authenticator.Key)!, authenticator.LastStep).ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Activates the administrator (created without a password) through the e-mailed code.
    /// </summary>
    /// <returns>A task.</returns>
    private async Task ActivateAdministratorAsync()
    {
        using var client = await ActivateAsync(OdcApplication.AdminEmail, AdminPassword);
    }

    /// <summary>
    /// An enrolled authenticator.
    /// </summary>
    /// <param name="key">The shared key (base32).</param>
    private sealed class Authenticator(string key)
    {
        /// <summary>
        /// Gets the shared key.
        /// </summary>
        public string Key { get; } = key;

        /// <summary>
        /// Gets or sets the last time step whose code was used.
        /// </summary>
        public long LastStep { get; set; } = long.MinValue;
    }
}
