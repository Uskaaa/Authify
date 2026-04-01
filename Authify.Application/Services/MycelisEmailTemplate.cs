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
            : $"<p class='text text-secondary'>{outro}</p>";

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width,initial-scale=1">
    <style>
        body { margin:0; padding:24px; background:#101214; color:#f5f7fb; font-family:'DM Sans', Arial, sans-serif; }
        .wrapper { max-width:560px; margin:0 auto; }
        .card { background:#171a1f; border:1px solid rgba(255,255,255,0.16); border-radius:8px; padding:32px 28px; }
        .brand { color:#9aa3b2; letter-spacing:0.08em; text-transform:uppercase; font-size:12px; margin:0 0 14px; }
        .title { font-family:'Syne','DM Sans',Arial,sans-serif; color:#f5f7fb; font-size:28px; line-height:1.2; margin:0 0 14px; }
        .text { color:#f5f7fb; font-size:15px; line-height:1.7; margin:0 0 14px; }
        .text-secondary { color:#c2c9d4; }
        .btn-wrap { text-align:center; margin:26px 0; }
        .btn-primary { display:inline-block; background:#f5f7fb; color:#101214 !important; border-radius:999px; padding:12px 26px; text-decoration:none; font-size:14px; font-weight:600; letter-spacing:0.02em; }
        .fallback { color:#9aa3b2; font-size:12px; line-height:1.5; word-break:break-all; margin:18px 0 0; }
        .fallback a { color:#f5f7fb; text-decoration:underline; }
        .footer { color:#9aa3b2; font-size:12px; margin:18px 0 0; padding-top:14px; border-top:1px solid rgba(255,255,255,0.16); }
    </style>
</head>
<body>
    <div class="wrapper">
        <div class="card">
            <p class="brand">Mycelis</p>
            <h1 class="title">{{title}}</h1>
            <p class="text">{{intro}}</p>
            <div class="btn-wrap">
                <a href="{{actionUrl}}" class="btn-primary">{{actionLabel}}</a>
            </div>
            {{outroBlock}}
            <p class="fallback">Falls der Button nicht funktioniert, nutze diesen Link:<br><a href="{{actionUrl}}">{{actionUrl}}</a></p>
            <p class="footer">Mycelis Security</p>
        </div>
    </div>
</body>
</html>
""";
    }

    public static string BuildOtpEmail(string otp)
    {
        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width,initial-scale=1">
    <style>
        body { margin:0; padding:24px; background:#101214; color:#f5f7fb; font-family:'DM Sans', Arial, sans-serif; }
        .wrapper { max-width:560px; margin:0 auto; }
        .card { background:#171a1f; border:1px solid rgba(255,255,255,0.16); border-radius:8px; padding:32px 28px; }
        .brand { color:#9aa3b2; letter-spacing:0.08em; text-transform:uppercase; font-size:12px; margin:0 0 14px; }
        .title { font-family:'Syne','DM Sans',Arial,sans-serif; color:#f5f7fb; font-size:28px; line-height:1.2; margin:0 0 14px; }
        .text { color:#c2c9d4; font-size:15px; line-height:1.7; margin:0 0 14px; }
        .otp { font-size:32px; font-weight:700; letter-spacing:6px; text-align:center; background:rgba(245,247,251,0.10); color:#f5f7fb; border:1px solid rgba(255,255,255,0.16); border-radius:8px; padding:14px 10px; margin:22px 0; }
        .footer { color:#9aa3b2; font-size:12px; margin:16px 0 0; padding-top:14px; border-top:1px solid rgba(255,255,255,0.16); }
    </style>
</head>
<body>
    <div class="wrapper">
        <div class="card">
            <p class="brand">Mycelis</p>
            <h1 class="title">Dein Bestätigungscode</h1>
            <p class="text">Nutze den folgenden Code, um deine Anmeldung abzuschließen:</p>
            <div class="otp">{{otp}}</div>
            <p class="text">Der Code ist 10 Minuten gültig.</p>
            <p class="footer">Mycelis Security</p>
        </div>
    </div>
</body>
</html>
""";
    }
}
