namespace KOD.Domain.Audit;

/// <summary>
/// Codes of audited actions.
/// </summary>
public static class AuditActions
{
    /// <summary>Successful sign-in.</summary>
    public const string SignIn = "auth.sign-in";

    /// <summary>Failed sign-in attempt.</summary>
    public const string SignInFailed = "auth.sign-in-failed";

    /// <summary>Account locked out after failed attempts.</summary>
    public const string LockedOut = "auth.locked-out";

    /// <summary>Sign-out.</summary>
    public const string SignOut = "auth.sign-out";

    /// <summary>Authenticator app enrolled.</summary>
    public const string AuthenticatorEnrolled = "auth.authenticator-enrolled";

    /// <summary>Authenticator reset.</summary>
    public const string AuthenticatorReset = "auth.authenticator-reset";

    /// <summary>Recovery codes regenerated.</summary>
    public const string RecoveryCodesGenerated = "auth.recovery-codes";

    /// <summary>Password changed by the user.</summary>
    public const string PasswordChanged = "auth.password-changed";

    /// <summary>Password reset by an administrator.</summary>
    public const string PasswordReset = "auth.password-reset";

    /// <summary>Session revoked.</summary>
    public const string SessionRevoked = "auth.session-revoked";

    /// <summary>User created.</summary>
    public const string UserCreated = "users.created";

    /// <summary>User updated.</summary>
    public const string UserUpdated = "users.updated";

    /// <summary>User blocked or unblocked.</summary>
    public const string UserStatusChanged = "users.status-changed";

    /// <summary>Users imported.</summary>
    public const string UsersImported = "users.imported";

    /// <summary>Users exported.</summary>
    public const string UsersExported = "users.exported";

    /// <summary>Role created or changed.</summary>
    public const string RoleChanged = "roles.changed";

    /// <summary>Role deleted.</summary>
    public const string RoleDeleted = "roles.deleted";

    /// <summary>Organization unit created or changed.</summary>
    public const string UnitChanged = "structure.changed";

    /// <summary>Unit type created or changed.</summary>
    public const string UnitTypeChanged = "structure.type-changed";

    /// <summary>Unit type deleted.</summary>
    public const string UnitTypeDeleted = "structure.type-deleted";

    /// <summary>Document created.</summary>
    public const string DocumentCreated = "documents.created";

    /// <summary>Document changed (details, route or file) by its author.</summary>
    public const string DocumentUpdated = "documents.updated";

    /// <summary>Document submitted for signing.</summary>
    public const string DocumentSubmitted = "documents.submitted";

    /// <summary>Document withdrawn from signing.</summary>
    public const string DocumentWithdrawn = "documents.withdrawn";

    /// <summary>Document signed.</summary>
    public const string DocumentSigned = "documents.signed";

    /// <summary>Signing declined.</summary>
    public const string DocumentRejected = "documents.rejected";

    /// <summary>Document archived.</summary>
    public const string DocumentArchived = "documents.archived";

    /// <summary>Document deleted.</summary>
    public const string DocumentDeleted = "documents.deleted";

    /// <summary>News article created, changed or published.</summary>
    public const string NewsChanged = "news.changed";

    /// <summary>News article deleted.</summary>
    public const string NewsDeleted = "news.deleted";
}
