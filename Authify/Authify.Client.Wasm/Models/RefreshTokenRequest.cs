namespace Authify.Client.Wasm.Models;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string DeviceName { get; set; } = "browser";
    public string IpAddress { get; set; } = "unknown";
    public bool RememberMe { get; set; } = false;
}