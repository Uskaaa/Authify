namespace Authify.UI.Models;

public class UserExportRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Fremdschlüssel zum Benutzer
    public string UserId { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public bool IsCompleted { get; set; } = false;
    public string? ExportFilePath { get; set; }
    public string? ExportFileName { get; set; }
}