using Microsoft.AspNetCore.Mvc;

namespace Home.Web.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "About";
            return View();
        }
    }
}
