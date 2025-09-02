using Authify.Core.Common;
using Authify.Core.Models;
using Authify.Core.Server.Models;

namespace Authify.Core.Interfaces;

public interface IAuthServiceJwt
{
    Task<OperationResult<(string AccessToken, string RefreshToken)?>> LoginAsync(LoginRequest request);
    Task<OperationResult<(string AccessToken, string RefreshToken)?>> VerifyOtpAsync(OtpVerificationRequest request);
    Task<OperationResult> ResendOtpAsync(ResendOtpRequest request);
    Task LogoutAsync(string refreshToken);
}