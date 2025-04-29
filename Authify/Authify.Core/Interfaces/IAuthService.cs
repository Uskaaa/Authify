using Authify.Core.Common;
using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface IAuthService
{
    Task<OperationResult> LoginAsync(LoginRequest request);
    Task<OperationResult> VerifyOtpAsync(OtpVerificationRequest request);
}