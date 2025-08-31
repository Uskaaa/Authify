using Authify.Core.Common;

namespace Authify.Core.Interfaces;

public interface IUserDataExportService
{
    /// <summary>
    /// Exportiert die Daten eines Benutzers und gibt den Pfad zur Datei zurück
    /// </summary>
    Task<OperationResult<string>> ExportUserDataAsync(string userId);
}