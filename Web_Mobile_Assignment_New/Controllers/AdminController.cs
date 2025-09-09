using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web_Mobile_Assignment_New.Controllers
{
    public class AdminController : Controller
    {
        private readonly DB _context;
        public AdminController(DB context)
        {
            _context = context;
        }

        public IActionResult UserManagement(int page=1, int pageSize=50)
        {
            var users = _context.Users.OrderBy(u => u.Email).Skip(page - 1).Take(pageSize).ToList();

            // calculate total pages
            var totalUsers = _context.Users.Count();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(users);
        }

        public IActionResult PropertyManagement()
        {
            var houses = _context.Houses.ToList();
            return View(houses);
        }

        public IActionResult UserDetails(string? email)
        {
            if (string.IsNullOrEmpty(email)) return NotFound();

            User? user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return NotFound();

            return View(user);
        }

        public IActionResult PropertyDetails(int id)
        {
            if (id is 0) return NotFound();

            House? house = _context.Houses.FirstOrDefault(h => h.Id == id);
            if (house == null) return NotFound();

            return View(house);
        }


        public IActionResult Update(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Update(user);
                _context.SaveChanges();
                return RedirectToAction("UserDetails", new { email = user.Email });
            }
            return View("UserDetails", user); // 
        }
        [HttpPost]
        public IActionResult DeletePhoto(string? email)
        {
            if (string.IsNullOrEmpty(email)) return NotFound();


            User? user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user is OwnerTenant otUser) // 👈 只有 Owner/Tenant 才有 PhotoURL
            {
                otUser.PhotoURL = null;
                _context.Users.Update(otUser);
                _context.SaveChanges();
            }

            return RedirectToAction("UserDetails", new { email = email });
        }

        public IActionResult DeleteUser(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Message"] = "No email provided.";
                return RedirectToAction("UserManagement");
            }

            User? user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                TempData["Message"] = "User deleted successfully!";
            }
            else
            {
                TempData["Message"] = "User not found.";
            }

            return RedirectToAction("UserManagement");
        }

        public IActionResult ReportManagement()
        {
            return View();
        }
    }
}
