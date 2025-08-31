using Authify.Core.Common;
using Authify.Core.Models;
using Authify.Core.Server.Models;

namespace Authify.Core.Interfaces;

public interface IUserService
{
    Task<OperationResult> RegisterAsync(RegisterRequest request);
    Task<OperationResult> ConfirmEmailAsync(EmailConfirmationRequest request);
    Task<OperationResult> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest request);
    Task<OperationResult> ChangePasswordAsync(string userId, ChangePasswordRequest request);
}