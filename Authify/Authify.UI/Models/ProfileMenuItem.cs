using Microsoft.AspNetCore.Components;

namespace Authify.UI.Models;

public class ProfileMenuItem
{
    public string Href { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public RenderFragment Icon { get; set; } 
    public int Order { get; set; } = 0;
}