namespace ODC.Application.Common.Email;

/// <summary>
/// How the connection to the SMTP server is protected.
/// </summary>
public enum EmailSecurity
{
    /// <summary>A plain connection upgraded with STARTTLS (port 587).</summary>
    StartTls = 0,

    /// <summary>TLS from the start (port 465).</summary>
    SslOnConnect = 1,

    /// <summary>No protection (a local relay only).</summary>
    None = 2,
}
