using System.Diagnostics;
using DosyaYonetimPortal.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace DosyaYonetimPortal.Web.Controllers;

public class FilesController : Controller
{
    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View("~/Views/Shared/Error.cshtml",
            new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
