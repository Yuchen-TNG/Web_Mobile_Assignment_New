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


        [HttpPost]
        public IActionResult UpdateUser(string Email, string Name, string BirthdayString)
        {
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == Email);
            if (existingUser == null)
            {
                TempData["Message"] = "System Error";
                return NotFound();
            }
            existingUser.Name = Name;

            if (DateOnly.TryParse(BirthdayString, out var birthday))
                existingUser.Birthday = birthday;
            else
            {
                ModelState.AddModelError("Birthday", "Invalid date format");
                TempData["Message"] = "System Error";
            }
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "System Error";
                return View("UserDetails", existingUser);
            }    
            _context.SaveChanges();
            TempData["Message"] = "User Change Successful!";
            return RedirectToAction("UserDetails", new { email = Email });
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
                TempData["Message"] = "Delete Successful";
            }
            TempData["Message"] = "Delete failed";
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
                TempData["Message"] = "User deleted Successful!";
            }
            else
            {
                TempData["Message"] = "User not found.";
            }

            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        [HttpPost]
        public IActionResult UpdateProperty(House model)
        {
            var existing = _context.Houses.FirstOrDefault(h => h.Id == model.Id);
            if (existing == null)
            {
                TempData["Message"] = "House not found";
                return RedirectToAction("PropertyManagement");
            }

            // 更新允许修改的字段
            existing.RoomName = model.RoomName;
            existing.RoomType = model.RoomType;
            existing.Rooms = model.Rooms;
            existing.Bathrooms = model.Bathrooms;
            existing.Furnishing = model.Furnishing;
            existing.Price = model.Price;
            existing.StartDate = model.StartDate;
            existing.EndDate = model.EndDate;
            existing.Address = model.Address;
            existing.Sqft = model.Sqft;
            existing.RoomStatus = model.RoomStatus;

            // 如果你希望图片也可以修改，需要确保前端有 Hidden Input
            if (!string.IsNullOrEmpty(model.ImageUrl))
                existing.ImageUrl = model.ImageUrl;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();
                TempData["Message"] = string.Join("; ", errors);
                return RedirectToAction("PropertyDetails", new { id = model.Id });
            }

            _context.SaveChanges();
            TempData["Message"] = "Property Change Successful";
            return RedirectToAction("PropertyDetails", new { id = model.Id });
        }


        public IActionResult PropertyDelete(int id)
        {
            House? house = _context.Houses.FirstOrDefault(h => h.Id == id);
            if (house != null)
            {
                _context.Houses.Remove(house);
                _context.SaveChanges();
                TempData["Message"] = "Deleted Susscesful.";
            }
            else
            {
                TempData["Message"] = "Deleted Failed, no releted house.";
            }
            return RedirectToAction("PropertyManagement");
        }

        public IActionResult ReportManagement()
        {
            return View();
        }
    }
}
