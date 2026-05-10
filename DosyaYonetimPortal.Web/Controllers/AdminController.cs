using Microsoft.AspNetCore.Mvc;

namespace DosyaYonetimPortal.Web.Controllers;

public class AdminController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Users() => View();

    public IActionResult Files() => View();
}
