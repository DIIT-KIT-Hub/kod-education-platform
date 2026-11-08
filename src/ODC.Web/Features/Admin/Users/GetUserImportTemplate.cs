using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Admin.Users;

/// <summary>
/// The import template with sample rows.
/// </summary>
public static class GetUserImportTemplate
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Format">The file format.</param>
    [RequirePermission(Permissions.Users.Import)]
    public sealed record Query(SpreadsheetFormat Format) : IQuery<Result<DownloadableFile>>;

    /// <summary>
    /// Writes the sample rows.
    /// </summary>
    /// <param name="spreadsheet">Writes the file.</param>
    internal sealed class Handler(IUserSpreadsheet spreadsheet) : IRequestHandler<Query, Result<DownloadableFile>>
    {
        /// <inheritdoc />
        public Task<Result<DownloadableFile>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            UserSheetRow[] sample =
            [
                new(0, "ivan.petrenko@example.com", "Петренко", "Іван", "Олегович", "+380501234567", "SALES", SystemRoles.HeadOfUnit, "Начальник відділу продажу", "активний"),
                new(0, "ivan.petrenko@example.com", "Петренко", "Іван", "Олегович", "+380501234567", "SALES", SystemRoles.Employee, "Менеджер з ключових клієнтів", "активний"),
                new(0, "olena.koval@example.com", "Коваль", "Олена", "Петрівна", null, "HR", SystemRoles.HrSpecialist, "Інспектор з кадрів", "активний"),
            ];

            return Task.FromResult<Result<DownloadableFile>>(UserDirectory.ToFile(spreadsheet.Write(sample, request.Format), "users-import-template", request.Format));
        }
    }

    /// <summary>
    /// <c>GET /api/v1/user-imports/template</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => UserDirectory.MapUserImports(api).MapGet("template", HandleAsync);

        /// <summary>
        /// Downloads the import template with sample rows.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="format">The file format.</param>
        /// <returns>The file.</returns>
        private static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> HandleAsync(
            IRequestHandler<Query, Result<DownloadableFile>> handler,
            CancellationToken cancellationToken,
            SpreadsheetFormat format = SpreadsheetFormat.Xlsx)
            => (await handler.HandleAsync(new Query(format), cancellationToken)).ToFile();
    }
}
