using Authify.Core.Extensions;
using Authify.Core.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Authify.Application.Services;

public class SmsSender : ISmsSender
{
    private readonly InfrastructureOptions _infrastructureOptions;

    public SmsSender(InfrastructureOptions infrastructureOptions)
    {
        _infrastructureOptions = infrastructureOptions;
        TwilioClient.Init(_infrastructureOptions.AccountSid, _infrastructureOptions.AuthToken);
    }

    public async Task SendSmsAsync(string destination, string otp)
    {
        await MessageResource.CreateAsync(
            body: $"Your OTP code is: {otp}",
            from: new Twilio.Types.PhoneNumber(_infrastructureOptions.FromNumber),
            to: new Twilio.Types.PhoneNumber(destination)
        );
    }
}