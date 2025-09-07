using Microsoft.AspNetCore.Mvc;

namespace Web_Mobile_Assignment_New.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult userManagement()
        {
            return View();
        }

        public IActionResult propertyManagement()
        {
            return View();
        }
    }
}
