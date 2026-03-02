<div align="center">

<img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10" />
<img src="https://img.shields.io/badge/Blazor-WASM%20%7C%20Server-5C2D91?style=for-the-badge&logo=blazor&logoColor=white" alt="Blazor" />
<img src="https://img.shields.io/badge/license-MIT-22c55e?style=for-the-badge" alt="MIT License" />

<h1>🔐 Authify</h1>

<p><strong>A plug-and-play authentication UI library for Blazor.</strong><br/>
Drop it into your WASM or Server project and get a complete, branded auth flow in minutes.</p>

</div>

---

## What is Authify?

Authify is a set of NuGet packages that provides a **ready-made authentication and user-profile UI** for Blazor applications. Instead of building login, registration and profile pages from scratch, you reference Authify, configure a few options and your app has a fully working auth flow — including dark mode, localization and OAuth.

### What's included

| Area | Details |
|---|---|
| **Auth pages** | Login, Register, OTP verification, Forgot Password, Reset Password, Confirm E-Mail |
| **Profile pages** | Profile Settings (avatar, name, …), Privacy Settings, Security Settings (password, 2FA, OAuth) |
| **Styling** | Tailwind-based scoped CSS + CSS custom properties for full theme control |
| **Branding** | Logo (icon, SVG or image) + primary color palette — all configurable from the host project |
| **Localization** | English and German out of the box |
| **Dark mode** | Automatic via `dark` class on `<html>` |

---

## Packages

```
Authify.UI              → Razor Class Library (pages, components, CSS)
Authify.Client.Wasm     → Blazor WebAssembly integration  (references Authify.UI)
Authify.Client.Server   → Blazor Server integration       (references Authify.UI)
Authify.Api             → ASP.NET Core backend REST API
```

For a **Blazor WASM** host project you reference **`Authify.Client.Wasm`**.  
For a **Blazor Server** host project you reference **`Authify.Client.Server`**.  
You never need to reference `Authify.UI` directly.

---

## Quick Start — Blazor WebAssembly

### 1. Install the package

```xml
<!-- YourApp.Client.csproj -->
<PackageReference Include="Authify.Client.Wasm" Version="*" />
```

### 2. Register services (`Program.cs`)

```csharp
builder.Services.AddAuthifyWasmUI(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});
```

### 3. Link static assets (`App.razor` or `index.html`)

```html
<!-- Default Authify theme (CSS custom properties) -->
<link rel="stylesheet" href="_content/Authify.UI/css/authify-theme.css" />

<!-- Compiled Tailwind utility bundle -->
<link rel="stylesheet" href="_content/Authify.UI/authify.bundle.css" />

<!-- Font Awesome (required for default icon logo and page icons) -->
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css" />
```

> **Tip:** Place these links **before** your own stylesheets so you can override them easily.

### 4. Add routes (`App.razor`)

Authify pages live in the `Authify.UI` assembly. Tell the Blazor router to scan it:

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

### 1. Install the package

```xml
<!-- YourApp.csproj -->
<PackageReference Include="Authify.Client.Server" Version="*" />
```

### 2. Register services (`Program.cs`)

```csharp
builder.Services.AddAuthifyServerUI<AppDbContext, AppUser>(options =>
{
    options.Domain           = "https://yourapp.com";
    options.ConnectionString = builder.Configuration.GetConnectionString("Default")!;

    // SMTP — required for e-mail confirmation and password reset
    options.SmtpHost     = "smtp.example.com";
    options.SmtpPort     = 587;
    options.SmtpUsername = "user@example.com";
    options.SmtpPassword = "••••••••";
    options.EnableSsl    = true;

    // OAuth providers — all optional
    options.GoogleClientId      = builder.Configuration["Auth:Google:ClientId"]!;
    options.GoogleClientSecret  = builder.Configuration["Auth:Google:ClientSecret"]!;

    options.GitHubClientId      = builder.Configuration["Auth:GitHub:ClientId"]!;
    options.GitHubClientSecret  = builder.Configuration["Auth:GitHub:ClientSecret"]!;

    options.FacebookAppId       = builder.Configuration["Auth:Facebook:AppId"]!;
    options.FacebookAppSecret   = builder.Configuration["Auth:Facebook:AppSecret"]!;
});
```

### 3. Register middleware (`Program.cs`)

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers(); // required for OAuth callback endpoints
```

### 4. Link static assets

Same as WASM — add the three `<link>` tags to your layout (`_Layout.cshtml` or `App.razor`):

```html
<link rel="stylesheet" href="_content/Authify.UI/css/authify-theme.css" />
<link rel="stylesheet" href="_content/Authify.UI/authify.bundle.css" />
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css" />
```

### 5. DbContext & User model

Your `AppDbContext` must implement `IAuthifyDbContext` and your `AppUser` must extend `ApplicationUser`:

```csharp
// AppUser.cs
public class AppUser : ApplicationUser { }

// AppDbContext.cs
public class AppDbContext : IdentityDbContext<AppUser>, IAuthifyDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
```

---

## Pages Reference

All pages are registered automatically once the router is configured.

| Route | Page | Description |
|---|---|---|
| `/login` | Login | Username/e-mail + password, remember me, OAuth buttons |
| `/register` | Register | New account with e-mail confirmation flow |
| `/otp` | OTP Verification | One-time-password input after login (if 2FA is active) |
| `/forgot-password` | Forgot Password | Sends a password-reset link via e-mail |
| `/reset-password` | Reset Password | Sets a new password using the link token |
| `/confirm-email` | Confirm E-Mail | Verifies the e-mail address from the confirmation link |
| `/profile-settings` | Profile Settings | Avatar, display name and account details |
| `/privacy-settings` | Privacy Settings | Data export and account deletion |
| `/security-settings` | Security Settings | Change password, manage 2FA methods, connected OAuth accounts |

> The three profile pages use `ProfileLayout` which includes the branded sidebar and mobile navigation.

---

## InfrastructureOptions (Server only)

All options are set via the `Action<InfrastructureOptions>` callback in `AddAuthifyServerUI`.

| Property | Type | Required | Description |
|---|---|---|---|
| `Domain` | `string` | ✅ | Public base URL of the app (used in e-mail links) |
| `ConnectionString` | `string` | ✅ | EF Core connection string |
| `SmtpHost` | `string` | ✅ | SMTP server hostname |
| `SmtpPort` | `int` | ✅ | SMTP port (typically `587` for STARTTLS) |
| `SmtpUsername` | `string` | ✅ | SMTP login username |
| `SmtpPassword` | `string` | ✅ | SMTP login password |
| `EnableSsl` | `bool` | — | Enable SSL/TLS (`true` by default) |
| `GoogleClientId` | `string` | — | Google OAuth Client ID |
| `GoogleClientSecret` | `string` | — | Google OAuth Client Secret |
| `GitHubClientId` | `string` | — | GitHub OAuth App Client ID |
| `GitHubClientSecret` | `string` | — | GitHub OAuth App Client Secret |
| `FacebookAppId` | `string` | — | Facebook App ID |
| `FacebookAppSecret` | `string` | — | Facebook App Secret |
| `AccountSid` | `string` | — | Twilio Account SID (SMS / phone 2FA) |
| `AuthToken` | `string` | — | Twilio Auth Token |
| `FromNumber` | `string` | — | Twilio sender number |

---

## Branding

Authify is fully brandable. Pass an optional second parameter to either registration method.

### WASM

```csharp
builder.Services.AddAuthifyWasmUI(
    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    brand =>
    {
        brand.AppName = "MyApp"; // shown in mobile top-bar
        brand.Logo    = /* see Logo options below */;
        brand.Theme   = /* see Theme options below */;
    });
```

### Blazor Server

```csharp
builder.Services.AddAuthifyServerUI<AppDbContext, AppUser>(
    opts => { /* InfrastructureOptions … */ },
    brand =>
    {
        brand.AppName = "MyApp";
        brand.Logo    = /* … */;
    });
```

---

### Logo Options

Three logo styles are available. Desktop sidebar and mobile header always show the **same** logo automatically.

#### Option A — Icon + Text *(default)*

A FontAwesome icon inside a gradient box, followed by a two-part brand name.

```csharp
brand.Logo = AuthifyLogoOptions.FromIcon(
    iconClass:     "fa-solid fa-rocket",  // any FontAwesome class
    textPrefix:    "My",                  // plain part of the name
    textHighlight: "App",                 // highlighted (primary-colored) part
    gradientFrom:  "auth-from-primary-500",  // optional – Tailwind gradient-from class
    gradientTo:    "auth-to-indigo-700"      // optional – Tailwind gradient-to class
);
```

#### Option B — SVG + Text

Provide raw inline SVG markup as the icon.

```csharp
brand.Logo = AuthifyLogoOptions.FromSvg(
    svgContent:    "<svg xmlns='…'>…</svg>",
    textPrefix:    "My",
    textHighlight: "App"
);
```

#### Option C — Image file (PNG / JPG / SVG)

```csharp
brand.Logo = AuthifyLogoOptions.FromImage(
    imageUrl: "/images/logo.svg",   // relative or absolute URL
    altText:  "MyApp",              // optional – defaults to AppName
    cssClass: "auth-h-7"            // optional – extra Tailwind classes on <img>
);
```

---

### Theme Options

Override the primary color palette and semantic colors at runtime — no CSS rebuild needed.

```csharp
brand.Theme.PrimaryPalette[50]  = "#fff1f2";
brand.Theme.PrimaryPalette[100] = "#ffe4e6";
brand.Theme.PrimaryPalette[200] = "#fecdd3";
brand.Theme.PrimaryPalette[300] = "#fda4af";
brand.Theme.PrimaryPalette[400] = "#fb7185";
brand.Theme.PrimaryPalette[500] = "#f43f5e";
brand.Theme.PrimaryPalette[600] = "#e11d48";  // main action color (buttons, links)
brand.Theme.PrimaryPalette[700] = "#be123c";  // hover state
brand.Theme.PrimaryPalette[800] = "#9f1239";
brand.Theme.PrimaryPalette[900] = "#881337";
brand.Theme.PrimaryPalette[950] = "#4c0519";

// Optional: semantic background overrides
brand.Theme.LightBackground    = "#fff1f2";  // page bg in light mode (default: primary-50)
brand.Theme.DarkBackground     = "#0c0a09";  // page bg in dark mode
brand.Theme.LightCardBackground = "#ffffff"; // card/form bg in light mode
brand.Theme.DarkCardBackground  = "#1c1917"; // card/form bg in dark mode
```

> **How it works:** `authify-theme.css` defines the default CSS custom properties.  
> `AuthifyThemeStyle` (rendered inside `ProfileLayout`) injects a `<style>` block that overrides those variables at runtime via the CSS cascade.  
> Both Tailwind utility classes (e.g. `auth-bg-primary-600/20`) and scoped page CSS (e.g. `var(--auth-primary)`) pick up the new values automatically — no rebuild required.

---

## Localization

Authify ships with **English** (default) and **German** resource files.  
Add your preferred culture to the host project's `Program.cs`:

```csharp
builder.Services.AddLocalization();

// Blazor WASM – set culture before rendering
CultureInfo.DefaultThreadCurrentCulture   = new CultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("de-DE");
```

---

## Dark Mode

Authify respects the `dark` class on the `<html>` element (same convention as Tailwind's `darkMode: 'class'`).  
Toggle dark mode by adding/removing the class via JavaScript:

```js
// turn on
document.documentElement.classList.add('dark');

// turn off
document.documentElement.classList.remove('dark');

// toggle
document.documentElement.classList.toggle('dark');
```

A built-in `applyTheme` JavaScript function is included in `authify.bundle.css` and is called automatically on first render to restore the user's last preference from `localStorage`.

---

## Project Structure

```
Authify.sln
├── Authify.Core            → Shared models, interfaces, DTOs
├── Authify.Application     → Business logic, Identity, EF Core
├── Authify.Api             → ASP.NET Core REST API backend
├── Authify.UI              → Razor Class Library (pages + components + CSS)
├── Authify.Client.Wasm     → WASM integration (services, auth state)
└── Authify.Client.Server   → Blazor Server integration (controllers, cookie auth)
```

---

<div align="center">

*This README was automatically generated with the assistance of GitHub Copilot.*

</div>
