using KOD.Application.Common.Files;
using KOD.Application.Features.Roles;
using KOD.Application.Features.Users;
using KOD.Domain.Access;
using KOD.Domain.Localization;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// Imports users from spreadsheets (with a dry run) and exports them (interactive server component).
/// </summary>
public partial class UsersImport
{
    /// <summary>
    /// The number of error rows per page.
    /// </summary>
    private const int ErrorPageSize = 20;

    /// <summary>
    /// The column headers of the import file; part of the file format, so they stay Ukrainian in every interface language.
    /// </summary>
    private const string ImportColumns = "Email, Прізвище, Ім'я, По батькові, Телефон, Код підрозділу, Код ролі, Посада, Статус";

    /// <summary>
    /// Gets or sets the role options; <see langword="null"/> until loaded. Persisted from prerendering, so the
    /// interactive render shows the same page at once instead of loading them again.
    /// </summary>
    [PersistentState]
    public IReadOnlyList<RoleOption>? RoleOptions { get; set; }

    /// <summary>
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the user service.
    /// </summary>
    [Inject]
    private IUserService Users { get; set; } = default!;

    /// <summary>
    /// Gets or sets the role service.
    /// </summary>
    [Inject]
    private IRoleService RoleService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the JavaScript runtime.
    /// </summary>
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Gets or sets the chosen file name.
    /// </summary>
    private string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the chosen file content.
    /// </summary>
    private byte[]? FileBytes { get; set; }

    /// <summary>
    /// Gets or sets the last report.
    /// </summary>
    private UserImportReport? Report { get; set; }

    /// <summary>
    /// Gets the role options.
    /// </summary>
    private IReadOnlyList<RoleOption> Roles => RoleOptions ?? [];

    /// <summary>
    /// Gets or sets the error page number.
    /// </summary>
    private int ErrorPageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether an operation is running.
    /// </summary>
    private bool Busy { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user can import.
    /// </summary>
    private bool CanImport { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user can export.
    /// </summary>
    private bool CanExport { get; set; }

    /// <summary>
    /// Gets the number of error pages.
    /// </summary>
    private int ErrorPages => Math.Max(1, (int)Math.Ceiling((Report?.Errors.Count ?? 0) / (double)ErrorPageSize));

    /// <summary>
    /// Gets the errors of the current page.
    /// </summary>
    private IReadOnlyList<ImportRowError> ErrorPage => [.. (Report?.Errors ?? []).Skip((ErrorPageNumber - 1) * ErrorPageSize).Take(ErrorPageSize)];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthenticationState).User;
        CanImport = user.HasPermission(Permissions.Users.Import);
        CanExport = user.HasPermission(Permissions.Users.Export);
        RoleOptions ??= await RoleService.GetOptionsAsync(CancellationToken.None);
    }

    /// <summary>
    /// Reads the chosen file into memory (at most 5 MB).
    /// </summary>
    /// <param name="args">The file selection.</param>
    /// <returns>A task that completes when the file is read.</returns>
    private async Task SelectFile(InputFileChangeEventArgs args)
    {
        Report = null;
        ErrorMessage = null;
        FileBytes = null;
        if (args.File.Size > FileRules.MaxImportSize)
        {
            ErrorMessage = Texts.T("Файл імпорту не може бути більшим за 5 МБ.");
            return;
        }

        FileName = args.File.Name;
        using var buffer = new MemoryStream();
        await using var stream = args.File.OpenReadStream(FileRules.MaxImportSize);
        await stream.CopyToAsync(buffer);
        FileBytes = buffer.ToArray();
    }

    /// <summary>
    /// Validates or applies the import.
    /// </summary>
    /// <param name="dryRun">Whether to only validate.</param>
    /// <returns>A task that completes when done.</returns>
    private async Task RunAsync(bool dryRun)
    {
        if (FileBytes is null || FileName is null)
        {
            return;
        }

        Busy = true;
        ErrorMessage = null;
        try
        {
            using var stream = new MemoryStream(FileBytes);
            var result = await Users.ImportAsync(new FileUpload(stream, FileName, string.Empty, FileBytes.Length), dryRun, CancellationToken.None);
            Report = result.IsSuccess ? result.Value : null;
            ErrorMessage = result.IsSuccess ? null : result.Error.Message;
            ErrorPageNumber = 1;
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>
    /// Downloads the one-time credentials of the created accounts.
    /// </summary>
    /// <param name="format">The file format.</param>
    /// <returns>A task that completes when the browser received the file.</returns>
    private async Task DownloadCredentialsAsync(SpreadsheetFormat format)
    {
        if (Report is null)
        {
            return;
        }

        var file = Users.BuildCredentialsFile(Report.Credentials, format);
        using var reference = new DotNetStreamReference(file.Content);
        await JS.InvokeVoidAsync("kodDownloadStream", file.FileName, file.ContentType, reference);
    }
}
