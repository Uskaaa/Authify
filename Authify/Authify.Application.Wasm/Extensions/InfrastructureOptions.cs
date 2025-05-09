namespace Authify.Application.Extensions;

public class InfrastructureOptions
{
    public string Domain { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;
    public string GitHubClientId { get; set; } = string.Empty;
    public string GitHubClientSecret { get; set; } = string.Empty;
}