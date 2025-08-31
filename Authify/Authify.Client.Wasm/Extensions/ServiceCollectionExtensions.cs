using System.Text;
using Authify.Application.Data;
using Authify.Client.Wasm.Services;
using Authify.UI.Server.Extensions;
using Authify.UI.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Authify.Client.Wasm.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthifyWasmUI(IServiceCollection services,
        Action<HttpClient> configureClient)
    {
        services.AddAuthifyUI();
        services.AddHttpClient<IAuthifyDataService, WasmDataService>(configureClient);
        
        return services;
    }
}