using Microsoft.AspNetCore.Mvc;

namespace Home.Web.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Reports";
            return View();
        }
    }
}
