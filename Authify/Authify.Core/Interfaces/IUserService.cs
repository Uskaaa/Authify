using Authify.Core.Common;
using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface IUserService
{
    Task<OperationResult> RegisterAsync(RegisterRequest request);
    Task<OperationResult> ConfirmEmailAsync(string userId, string token);
    Task<OperationResult> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest request);
}