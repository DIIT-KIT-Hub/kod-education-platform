using System.ComponentModel.DataAnnotations;
using KOD.Application.Features.Users;

namespace KOD.Web.Api.Contracts;

/// <summary>
/// Body of <c>POST /api/v1/user-imports/credentials</c>: the temporary passwords of an applied import.
/// </summary>
/// <param name="Credentials">The credentials.</param>
public sealed record CredentialsFileRequest([Required][MaxLength(10_000)] IReadOnlyList<UserCredentials> Credentials);
