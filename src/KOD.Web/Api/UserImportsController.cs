using KOD.Application.Common.Files;
using KOD.Application.Features.Roles;
using KOD.Application.Features.Users;
using KOD.Domain.Access;
using KOD.Web.Api.Contracts;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// User imports from XLSX/CSV files. Each POST creates an import (or validates it when <c>dryRun=true</c>).
/// </summary>
/// <param name="users">The user service.</param>
/// <param name="roles">The role service.</param>
[Route("api/v1/user-imports")]
[RequirePermission(Permissions.Users.Import)]
public sealed class UserImportsController(IUserService users, IRoleService roles) : ApiControllerBase
{
    /// <summary>
    /// Lists the role codes the import file may use.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The roles.</returns>
    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleOption>>> Roles(CancellationToken cancellationToken)
        => Ok(await roles.GetOptionsAsync(cancellationToken));

    /// <summary>
    /// Builds the file of the temporary passwords an applied import has just returned (they are not stored, so the
    /// browser sends them back once to get a spreadsheet).
    /// </summary>
    /// <param name="request">The credentials.</param>
    /// <param name="format">The file format.</param>
    /// <returns>The file.</returns>
    [HttpPost("credentials")]
    [Produces("application/octet-stream")]
    public ActionResult Credentials(CredentialsFileRequest request, [FromQuery] SpreadsheetFormat format = SpreadsheetFormat.Xlsx)
    {
        ArgumentNullException.ThrowIfNull(request);
        var file = users.BuildCredentialsFile(request.Credentials, format);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Imports users. Nothing is saved when any row is invalid.
    /// </summary>
    /// <param name="file">The spreadsheet (.xlsx or .csv).</param>
    /// <param name="dryRun">Whether to only validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The import report (201 when applied, 200 for dry runs or rejected files).</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileRules.MaxImportSize + (64 * 1024))]
    public async Task<ActionResult<UserImportReport>> Create(IFormFile file, [FromQuery] bool dryRun, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var content = file.OpenReadStream();
        var result = await users.ImportAsync(new FileUpload(content, file.FileName, file.ContentType, file.Length), dryRun, cancellationToken);
        if (!result.IsSuccess)
        {
            return ProblemFor(result.Error);
        }

        return result.Value.Applied ? StatusCode(StatusCodes.Status201Created, result.Value) : Ok(result.Value);
    }

    /// <summary>
    /// Downloads the import template with sample rows.
    /// </summary>
    /// <param name="format">The file format.</param>
    /// <returns>The file.</returns>
    [HttpGet("template")]
    [Produces("application/octet-stream")]
    public ActionResult Template([FromQuery] SpreadsheetFormat format = SpreadsheetFormat.Xlsx)
        => FromFile(users.GetImportTemplate(format));
}
