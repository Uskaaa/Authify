using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

public class OidcProviderController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}