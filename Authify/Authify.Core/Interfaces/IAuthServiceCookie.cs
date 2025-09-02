using Authify.Core.Common;
using Authify.Core.Models;
using Authify.Core.Server.Models;

namespace Authify.Core.Interfaces;

public interface IAuthServiceCookie
{
    Task<OperationResult<string>> LoginAsync(LoginRequest request);
    Task<OperationResult<string>> VerifyOtpAsync(OtpVerificationRequest request);
    Task<OperationResult> ResendOtpAsync(ResendOtpRequest request);
    Task<OperationResult> LogoutAsync();
}