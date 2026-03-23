using System.Security.Claims;
using Authify.Application.Data;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Authify.Application.Services;

public class AuthifyUserClaimsPrincipalFactory<TUser> : UserClaimsPrincipalFactory<TUser, IdentityRole>
    where TUser : ApplicationUser
{
    private readonly ITeamService? _teamService;

    public AuthifyUserClaimsPrincipalFactory(
        UserManager<TUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IServiceProvider serviceProvider)
        : base(userManager, roleManager, optionsAccessor)
    {
        // Optionaler Resolve um Modularität zu wahren
        _teamService = serviceProvider.GetService(typeof(ITeamService)) as ITeamService;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(TUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (_teamService != null)
        {
            var isMemberResult = await _teamService.IsTeamMemberAsync(user.Id);
            if (isMemberResult.Success && isMemberResult.Data)
            {
                identity.AddClaim(new Claim("is_team_member", "true"));
                
                var isAdminResult = await _teamService.IsTeamAdminAsync(user.Id);
                if (isAdminResult.Success && isAdminResult.Data)
                {
                    identity.AddClaim(new Claim("is_team_admin", "true"));
                }
            }
        }

        return identity;
    }
}
