using KOD.Application.Common.Files;
using KOD.Application.Features.Users;
using KOD.Domain.Access;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// User imports from XLSX/CSV files. Each POST creates an import (or validates it when <c>dryRun=true</c>).
/// </summary>
/// <param name="users">The user service.</param>
[Route("api/v1/user-imports")]
[RequirePermission(Permissions.Users.Import)]
public sealed class UserImportsController(IUserService users) : ApiControllerBase
{
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
