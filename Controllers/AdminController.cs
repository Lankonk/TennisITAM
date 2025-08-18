using Microsoft.AspNetCore.Mvc;

namespace TennisITAM.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
