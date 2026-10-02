using Authify.UI.Models;

namespace Authify.UI.Services;

/// <summary>
/// Extension point for the profile navigation. A library that ships its own profile pages
/// (e.g. billing) registers an implementation with
/// <c>services.TryAddEnumerable(ServiceDescriptor.Singleton&lt;IProfileMenuContributor, X&gt;())</c>;
/// Authify shows the items when that library is active without referencing it. Register it on
/// every render side (WASM client and server) since the same components render on both.
/// Implementations must be stateless and cheap: GetItems is called on every nav render.
/// </summary>
public interface IProfileMenuContributor
{
    IEnumerable<ProfileMenuItem> GetItems();
}
