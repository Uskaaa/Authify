using Authify.Core.Common;
using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface IAuthServiceCookie
{
    // ---- Legacy helpers (kept for backward compatibility) ----
    [System.Obsolete("Use ValidateCredentialsAsync + CompleteSignInAsync for the HTTP redirect pattern.")]
    Task<OperationResult<string>> LoginAsync(LoginRequest request);

    [System.Obsolete("Use ValidateOtpAsync + CompleteSignInAsync for the HTTP redirect pattern.")]
    Task<OperationResult<string>> VerifyOtpAsync(OtpVerificationRequest request);

    Task<OperationResult> ResendOtpAsync(ResendOtpRequest request);
    Task<OperationResult> LogoutAsync();

    // ---- HTTP redirect pattern (split validation from SignInAsync) ----

    /// <summary>
    /// Validates email/password and 2-FA state WITHOUT calling SignInAsync.
    /// If credentials are valid and no 2-FA is required, returns a <see cref="PendingLoginResult"/>
    /// with <c>UserId</c> and <c>IsPersistent</c> set.
    /// If 2-FA is required, sets <c>RequiresOtp = true</c> and <c>OtpToken</c>.
    /// </summary>
    Task<OperationResult<PendingLoginResult>> ValidateCredentialsAsync(LoginRequest request);

    /// <summary>
    /// Validates an OTP code WITHOUT calling SignInAsync.
    /// On success returns a <see cref="PendingLoginResult"/> with <c>UserId</c> and
    /// <c>IsPersistent</c> so the caller can complete sign-in via <see cref="CompleteSignInAsync"/>.
    /// </summary>
    Task<OperationResult<PendingLoginResult>> ValidateOtpAsync(OtpVerificationRequest request);

    /// <summary>
    /// Calls <c>SignInManager.SignInAsync</c> for the given user.
    /// Must be invoked from within a real HTTP request context so that
    /// ASP.NET Core Identity can write the HttpOnly auth cookie to the response.
    /// </summary>
    Task<OperationResult> CompleteSignInAsync(string userId, bool isPersistent);
}