using Authify.UI.Models;

namespace Authify.UI.Services;

internal sealed class NullUiCultureProvider : IUiCultureProvider
{
    public IReadOnlyList<UiCulture> GetSupportedCultures() => [];

    public Task SetCultureAsync(string cultureCode) => Task.CompletedTask;
}
