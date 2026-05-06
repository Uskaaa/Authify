namespace Authify.Application.Services;

internal static class MycelisEmailTemplate
{
    public static string BuildActionEmail(
        string title,
        string intro,
        string actionLabel,
        string actionUrl,
        string? outro = null)
    {
        var outroBlock = string.IsNullOrWhiteSpace(outro)
            ? string.Empty
            : $"<p style=\"margin:0 0 24px;color:#9ca3af;font-size:15px;line-height:1.6;\">{outro}</p>";

        return WrapHtml($"""
            <h2 style="margin:0 0 16px;color:#fff;font-size:20px;font-weight:600;">{title}</h2>
            <p style="margin:0 0 24px;color:#d1d5db;font-size:15px;line-height:1.6;">{intro}</p>
            <div style="margin-top:32px;margin-bottom:24px;">
              <a href="{actionUrl}" style="display:inline-block;background:#6366f1;color:#fff;text-decoration:none;padding:12px 24px;border-radius:8px;font-size:15px;font-weight:600;">
                {actionLabel}
              </a>
            </div>
            {outroBlock}
            <p style="color:#4b5563;font-size:12px;margin-top:24px;word-break:break-all;">
              If the button doesn't work, use this link:<br>
              <a href="{actionUrl}" style="color:#6366f1;text-decoration:underline;">{actionUrl}</a>
            </p>
            """);
    }

    public static string BuildOtpEmail(string otp)
    {
        return WrapHtml($"""
            <h2 style="margin:0 0 16px;color:#fff;font-size:20px;font-weight:600;">Verification Code</h2>
            <p style="margin:0 0 24px;color:#9ca3af;font-size:15px;line-height:1.6;">Use the following code to complete your login:</p>
            <div style="background:#111;border:1px solid #2a2a2a;border-radius:12px;padding:24px;margin-bottom:24px;text-align:center;">
              <div style="color:#9ca3af;font-size:13px;text-transform:uppercase;letter-spacing:0.05em;margin-bottom:8px;">Your Code</div>
              <div style="color:#fff;font-size:32px;font-weight:700;letter-spacing:6px;">{otp}</div>
            </div>
            <p style="margin:0 0 24px;color:#9ca3af;font-size:14px;line-height:1.6;">
              This code is valid for 10 minutes.
            </p>
            """);
    }

    private static string WrapHtml(string innerContent)
    {
        return $"""
            <div style="background-color:#050505;padding:40px 20px;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
              <div style="max-width:600px;margin:0 auto;background:#0a0a0a;border:1px solid #1f1f1f;border-radius:16px;overflow:hidden;">
                <div style="padding:32px;border-bottom:1px solid #1f1f1f;text-align:center;">
                  <img src="https://mycelis.com/logo-email.png" alt="Mycelis" style="height:32px;display:inline-block;">
                </div>
                <div style="padding:40px 32px;">
                  {innerContent}
                </div>
                <div style="padding:32px;background:#0d0d0d;border-top:1px solid #1f1f1f;text-align:center;">
                  <p style="margin:0;color:#4b5563;font-size:12px;">
                    &copy; {DateTime.UtcNow.Year} Mycelis. All rights reserved.<br>
                    You are receiving this email because of your active Mycelis account.
                  </p>
                </div>
              </div>
            </div>
            """;
    }
}
