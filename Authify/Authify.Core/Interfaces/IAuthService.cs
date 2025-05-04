using Authify.Core.Common;
using Authify.Core.Models;
using Authify.Core.Server.Models;

namespace Authify.Core.Interfaces;

public interface IAuthService
{
    Task<OperationResult<string>> LoginAsync(LoginRequest request);
    Task<OperationResult> VerifyOtpAsync(OtpVerificationRequest request);
}