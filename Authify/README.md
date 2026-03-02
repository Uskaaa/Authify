# Authify

Authify is a Blazor authentication library that provides a ready-made auth and user-profile UI for Blazor WASM and Blazor Server projects. Instead of building login, registration and profile pages from scratch, you add the package, configure a few options and the pages are there.

Built on top of ASP.NET Core Identity. Supports OAuth (Google, GitHub, Facebook), two-factor authentication, e-mail confirmation, password reset and more.

---

## What's included

| Area | Details |
|---|---|
| **Auth pages** | Login, Register, OTP verification, Forgot Password, Reset Password, Confirm E-Mail |
| **Profile pages** | Profile Settings, Privacy Settings, Security Settings (password, 2FA, OAuth) |
| **Styling** | Tailwind-based scoped CSS + CSS custom properties for full theme control |
| **Branding** | Logo (icon, SVG or image) + primary color palette — configurable from the host project |
| **Localization** | English and German out of the box |
| **Dark mode** | Via `dark` class on `<html>` |

---

## Packages

```
Authify.UI              → Razor Class Library (pages, components, CSS)
Authify.Client.Wasm     → Blazor WebAssembly integration
Authify.Client.Server   → Blazor Server integration
Authify.Api             → ASP.NET Core backend REST API
```

For a **Blazor WASM** host project reference `Authify.Client.Wasm`.  
For a **Blazor Server** host project reference `Authify.Client.Server`.  
You don't need to reference `Authify.UI` directly.

---

## Quick Start — Blazor WebAssembly

### 1. Add the package

```xml
<PackageReference Include="Authify.Client.Wasm" Version="*" />
```

### 2. Register services (`Program.cs`)

```csharp
builder.Services.AddAuthifyWasmUI(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});
```

### 3. Link static assets (`App.razor`)

```html
<link rel="stylesheet" href="_content/Authify.UI/css/authify-theme.css" />
<link rel="stylesheet" href="_content/Authify.UI/authify.bundle.css" />
<!-- Font Awesome — needed for the default icon logo and page icons -->
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css" />
```

### 4. Add routes (`App.razor`)

Authify pages live in the `Authify.UI` assembly. Add it to the router:

```razor
<Router AppAssembly="typeof(App).Assembly"
        AdditionalAssemblies="new[] { typeof(Authify.UI.Components.ProfileLayout).Assembly }">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
    </Found>
</Router>
```

---

## Quick Start — Blazor Server

### 1. Add the package

```xml
<PackageReference Include="Authify.Client.Server" Version="*" />
```

### 2. Register services (`Program.cs`)

```csharp
builder.Services.AddAuthifyServerUI<AppDbContext, AppUser>(options =>
{
    options.Domain           = "https://yourapp.com";
    options.ConnectionString = builder.Configuration.GetConnectionString("Default")!;

    options.SmtpHost     = "smtp.example.com";
    options.SmtpPort     = 587;
    options.SmtpUsername = "user@example.com";
    options.SmtpPassword = "••••••••";
    options.EnableSsl    = true;

    // OAuth — all optional
    options.GoogleClientId     = builder.Configuration["Auth:Google:ClientId"]!;
    options.GoogleClientSecret = builder.Configuration["Auth:Google:ClientSecret"]!;

    options.GitHubClientId     = builder.Configuration["Auth:GitHub:ClientId"]!;
    options.GitHubClientSecret = builder.Configuration["Auth:GitHub:ClientSecret"]!;

    options.FacebookAppId      = builder.Configuration["Auth:Facebook:AppId"]!;
    options.FacebookAppSecret  = builder.Configuration["Auth:Facebook:AppSecret"]!;
});
```

### 3. Middleware (`Program.cs`)

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers(); // required for OAuth callback endpoints
```

### 4. Link static assets

Same as WASM — add the three `<link>` tags to your layout.

### 5. DbContext & User model

```csharp
public class AppUser : ApplicationUser { }

public class AppDbContext : IdentityDbContext<AppUser>, IAuthifyDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
```

---

## Pages

| Route | Page | Description |
|---|---|---|
| `/login` | Login | E-mail + password, remember me, OAuth buttons |
| `/register` | Register | New account with e-mail confirmation |
| `/otp` | OTP | One-time-password input (if 2FA is active) |
| `/forgot-password` | Forgot Password | Sends a reset link via e-mail |
| `/reset-password` | Reset Password | Sets a new password using the token from the link |
| `/confirm-email` | Confirm E-Mail | Verifies the e-mail from the confirmation link |
| `/profile-settings` | Profile Settings | Avatar, display name, account details |
| `/privacy-settings` | Privacy Settings | Data export and account deletion |
| `/security-settings` | Security Settings | Change password, 2FA methods, connected OAuth accounts |

The three profile pages use `ProfileLayout` which includes the branded sidebar and mobile navigation.

---

## InfrastructureOptions (Blazor Server)

| Property | Required | Description |
|---|---|---|
| `Domain` | ✅ | Public base URL — used in e-mail links |
| `ConnectionString` | ✅ | EF Core connection string |
| `SmtpHost` | ✅ | SMTP server hostname |
| `SmtpPort` | ✅ | SMTP port (`587` for STARTTLS) |
| `SmtpUsername` | ✅ | SMTP login username |
| `SmtpPassword` | ✅ | SMTP login password |
| `EnableSsl` | — | Default: `true` |
| `GoogleClientId/Secret` | — | Google OAuth |
| `GitHubClientId/Secret` | — | GitHub OAuth |
| `FacebookAppId/Secret` | — | Facebook OAuth |
| `AccountSid / AuthToken / FromNumber` | — | Twilio for SMS-based 2FA |

---

## Branding

Both `AddAuthifyWasmUI` and `AddAuthifyServerUI` accept an optional second parameter for branding. If you skip it, the default Authify look is used.

```csharp
builder.Services.AddAuthifyWasmUI(
    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    brand =>
    {
        brand.AppName = "MyApp"; // shown in the mobile header
        brand.Logo    = /* see below */;

        // primary color palette — override any of the 11 shades (50–950)
        brand.Theme.PrimaryPalette[600] = "#e11d48";
        brand.Theme.PrimaryPalette[500] = "#f43f5e";
    });
```

### Logo

**Option A — Icon + Text** (default style)
```csharp
brand.Logo = AuthifyLogoOptions.FromIcon(
    iconClass:     "fa-solid fa-rocket",
    textPrefix:    "My",
    textHighlight: "App"   // rendered in the primary color
);
```

**Option B — SVG + Text**
```csharp
brand.Logo = AuthifyLogoOptions.FromSvg(
    svgContent:    "<svg xmlns='…'>…</svg>",
    textPrefix:    "My",
    textHighlight: "App"
);
```

**Option C — Image file**
```csharp
brand.Logo = AuthifyLogoOptions.FromImage(
    imageUrl: "/images/logo.svg",
    altText:  "MyApp"
);
```

Desktop sidebar and mobile header always show the **same** logo — no separate configuration needed.

### Theme colors

All primary color shades reference CSS custom properties, so changing the palette takes effect at runtime without any CSS rebuild.

```csharp
brand.Theme.PrimaryPalette[50]  = "#fff1f2";
brand.Theme.PrimaryPalette[100] = "#ffe4e6";
// … shades 200–500 …
brand.Theme.PrimaryPalette[600] = "#e11d48"; // main buttons, links
brand.Theme.PrimaryPalette[700] = "#be123c"; // hover state
// … shades 800–950 …

// optional semantic overrides
brand.Theme.LightBackground     = "#fff1f2";
brand.Theme.DarkBackground      = "#0c0a09";
brand.Theme.LightCardBackground = "#ffffff";
brand.Theme.DarkCardBackground  = "#1c1917";
```

---

## Localization

English and German resource files are included. Set the culture in `Program.cs`:

```csharp
CultureInfo.DefaultThreadCurrentCulture   = new CultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("de-DE");
```

---

## Dark Mode

Add or remove the `dark` class on `<html>`:

```js
document.documentElement.classList.add('dark');    // on
document.documentElement.classList.remove('dark'); // off
document.documentElement.classList.toggle('dark'); // toggle
```

A built-in `applyTheme` script restores the user's last preference from `localStorage` on first render automatically.

---

## Project Structure

```
Authify.sln
├── Authify.Core            → Shared models, interfaces, DTOs
├── Authify.Application     → Business logic, Identity, EF Core
├── Authify.Api             → ASP.NET Core backend REST API
├── Authify.UI              → Razor Class Library (pages + components + CSS)
├── Authify.Client.Wasm     → WASM integration (services, auth state)
└── Authify.Client.Server   → Blazor Server integration (controllers, cookie auth)
```

---

*This README was automatically generated with the assistance of GitHub Copilot.*
