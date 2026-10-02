namespace Authify.Core.Models;

/// <summary>
/// Data needed by the client to let a user add this account to an authenticator app
/// (Google Authenticator, Authy, 1Password, ...) and confirm the setup with a generated code.
/// </summary>
public class TotpSetupInfo
{
    /// <summary>The raw shared secret, formatted in groups of 4 characters for manual entry.</summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>otpauth:// URI encoding the secret, issuer and account name, for QR scanning.</summary>
    public string AuthenticatorUri { get; set; } = string.Empty;

    /// <summary>The AuthenticatorUri rendered as a QR code PNG, as a data: URI ready for an &lt;img&gt; tag.</summary>
    public string QrCodeImageDataUri { get; set; } = string.Empty;
}
