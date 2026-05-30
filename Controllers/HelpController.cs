using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WMS.Controllers;

[Authorize]
public class HelpController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
