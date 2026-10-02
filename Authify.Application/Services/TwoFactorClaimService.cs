using System.Security.Claims;
using System.Text;
using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Extensions;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Authify.Application.Services;

public class TwoFactorClaimService<TUser> : ITwoFactorClaimService
    where TUser : ApplicationUser
{
    private readonly UserManager<TUser> _userManager;
    private readonly IAuthifyDbContext _context;
    private readonly InfrastructureOptions _infrastructureOptions;

    public TwoFactorClaimService(UserManager<TUser> userManager, IAuthifyDbContext context, InfrastructureOptions infrastructureOptions)
    {
        _userManager = userManager;
        _context = context;
        _infrastructureOptions = infrastructureOptions;
    }

    // Hinzufügen oder Aktualisieren einer Methode
    public async Task<OperationResult> AddOrUpdateAsync(string userId, TwoFactorRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        // Prüfen, ob die Methode tatsächlich verwendet werden kann
        switch (request.TwoFactorMethod)
        {
            case TwoFactorMethod.Sms:
                if (string.IsNullOrEmpty(user.PhoneNumber) || !user.PhoneNumberConfirmed)
                    return OperationResult.Fail("Phone number is not set or confirmed.");
                break;
            case TwoFactorMethod.Email:
                if (string.IsNullOrEmpty(user.Email) || !user.EmailConfirmed)
                    return OperationResult.Fail("Email is not set or confirmed.");
                break;
            case TwoFactorMethod.Totp:
                // Totp can only be (re-)enabled/prioritized here once initial setup has been
                // confirmed via ConfirmTotpAsync, which is what actually creates this row.
                var hasConfirmedTotp = await _context.UserTwoFactors
                    .AnyAsync(x => x.UserId == userId && x.Method == TwoFactorMethod.Totp);
                if (!hasConfirmedTotp)
                    return OperationResult.Fail("Authenticator app is not set up yet. Complete setup first.");
                break;
            default:
                return OperationResult.Fail("Unknown two-factor method.");
        }

        // Wenn diese Methode als Default (Priority 0) gesetzt wird, alle anderen zurücksetzen
        if (request.Priority == 0)
        {
            var others = await _context.UserTwoFactors
                .Where(x => x.UserId == userId && x.Method != request.TwoFactorMethod && x.Priority == 0)
                .ToListAsync();
            foreach (var other in others)
                other.Priority = 1;
        }

        var existing = await _context.UserTwoFactors
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Method == request.TwoFactorMethod);

        if (existing != null)
        {
            existing.IsEnabled = request.IsEnabled;
            existing.Priority = request.Priority;
        }
        else
        {
            await _context.UserTwoFactors.AddAsync(new UserTwoFactor
            {
                UserId = userId,
                Method = request.TwoFactorMethod,
                IsEnabled = request.IsEnabled,
                Priority = request.Priority
            });
        }

        await _context.SaveChangesAsync();
        return OperationResult.Ok();
    }

    // Entfernen einer Methode
    public async Task<OperationResult> RemoveAsync(string userId, TwoFactorRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var existing = await _context.UserTwoFactors
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Method == request.TwoFactorMethod);

        if (existing == null)
            return OperationResult.Fail("TwoFactor method not found.");

        _context.UserTwoFactors.Remove(existing);
        await _context.SaveChangesAsync();

        // Invalidate the shared secret so a previously-shown QR/key can't be used to re-derive
        // codes if the user re-enables TOTP later without noticing the old secret leaked.
        if (request.TwoFactorMethod == TwoFactorMethod.Totp)
            await _userManager.ResetAuthenticatorKeyAsync(user);

        return OperationResult.Ok();
    }

    // Alle Methoden eines Users abfragen
    public async Task<OperationResult<List<UserTwoFactor>>> GetAllAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult<List<UserTwoFactor>>.Fail("User not found.");

        var methods = await _context.UserTwoFactors
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Priority)
            .ToListAsync();

        return methods.Count == 0
            ? OperationResult<List<UserTwoFactor>>.Fail("No two-factor methods found for the user.")
            : OperationResult<List<UserTwoFactor>>.Ok(methods);
    }

    // Bevorzugte Methode abfragen
    public async Task<OperationResult<UserTwoFactor>> GetPreferredAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult<UserTwoFactor>.Fail("User not found.");

        var preferred = await _context.UserTwoFactors
            .Where(x => x.UserId == userId && x.IsEnabled)
            .OrderBy(x => x.Priority)
            .FirstOrDefaultAsync();

        return preferred == null
            ? OperationResult<UserTwoFactor>.Fail("No enabled two-factor method found.")
            : OperationResult<UserTwoFactor>.Ok(preferred);
    }

    // Authenticator-app (TOTP) setup + confirmation.
    // Unlike Email/Sms, nothing is "sent" here — the secret is shared once via QR/manual key,
    // and the user's app derives codes from it locally. We only persist the UserTwoFactor row
    // once a real code from the app has been verified, so a user can't get "enabled" on a key
    // they never actually scanned successfully.

    public async Task<OperationResult<TotpSetupInfo>> GetTotpSetupInfoAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult<TotpSetupInfo>.Fail("User not found.");

        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var issuer = GetIssuer();
        var accountName = !string.IsNullOrEmpty(user.Email) ? user.Email : user.UserName ?? userId;
        var authenticatorUri = BuildAuthenticatorUri(issuer, accountName, unformattedKey!);

        return OperationResult<TotpSetupInfo>.Ok(new TotpSetupInfo
        {
            SharedKey = FormatKey(unformattedKey!),
            AuthenticatorUri = authenticatorUri,
            QrCodeImageDataUri = BuildQrCodeDataUri(authenticatorUri)
        });
    }

    public async Task<OperationResult> ConfirmTotpAsync(string userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var normalizedCode = code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, normalizedCode);

        if (!isValid)
            return OperationResult.Fail("Invalid authenticator code.");

        var existing = await _context.UserTwoFactors
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Method == TwoFactorMethod.Totp);

        if (existing != null)
        {
            existing.IsEnabled = true;
        }
        else
        {
            var hasOtherDefault = await _context.UserTwoFactors
                .AnyAsync(x => x.UserId == userId && x.Priority == 0);

            await _context.UserTwoFactors.AddAsync(new UserTwoFactor
            {
                UserId = userId,
                Method = TwoFactorMethod.Totp,
                IsEnabled = true,
                Priority = hasOtherDefault ? 1 : 0
            });
        }

        await _context.SaveChangesAsync();
        return OperationResult.Ok();
    }

    private string GetIssuer()
    {
        if (string.IsNullOrWhiteSpace(_infrastructureOptions.Domain))
            return "Authify";

        return Uri.TryCreate(_infrastructureOptions.Domain, UriKind.Absolute, out var uri)
            ? uri.Host
            : _infrastructureOptions.Domain;
    }

    private static string BuildAuthenticatorUri(string issuer, string accountName, string unformattedKey) =>
        $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}" +
        $"?secret={unformattedKey}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        for (var i = 0; i < unformattedKey.Length; i += 4)
        {
            result.Append(unformattedKey.AsSpan(i, Math.Min(4, unformattedKey.Length - i))).Append(' ');
        }
        return result.ToString().TrimEnd();
    }

    private static string BuildQrCodeDataUri(string authenticatorUri)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(qrCodeData);
        var bytes = pngQrCode.GetGraphic(10);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}