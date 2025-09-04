using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Web_Mobile_Assignment_New.Models;

namespace Web_Mobile_Assignment_New.Controllers
{
    public class HomeController : Controller
    {
        private readonly DB _context;

        public HomeController(DB context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            Console.WriteLine(_context.Database.GetDbConnection().ConnectionString);
            var houses = _context.Houses.ToList(); // 拿所有房子
            return View(houses);
        }

        [HttpGet]
        public IActionResult AddHouse()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddHouse(House house, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // 确保 wwwroot/images 存在
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // 保存文件
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    // 存路径到数据库 (相对路径)
                    house.ImageUrl = "/images/" + fileName;
                }

                _context.Houses.Add(house);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index"); // 添加后跳去主页面
            }
            return View(house);
        }


        public IActionResult Details(int id)
        {
            var house = _context.Houses.FirstOrDefault(h => h.Id == id);
            if (house == null) return NotFound();
            return View(house);
        }

        // GET: Home/Both
        [Authorize]
        public IActionResult Both()
        {
            return View();
        }

        // GET: Home/Owner
        [Authorize(Roles = "Owner")]
        public IActionResult Owner()
        {
            return View();
        }

        // GET: Home/Admin
        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            return View();
        }

        // GET: Home/Tenant
        [Authorize(Roles = "Tenant")]
        public IActionResult Rent(int id)
        {
            var house = _context.Houses.FirstOrDefault(h => h.Id == id);
            if (house == null) return NotFound();
            return View(house); // This will look for Views/Home/Rent.cshtml
        }

        [HttpPost]
        public IActionResult ConfirmRent(int id, DateTime startDate, DateTime endDate)
        {
            // Validate dates (e.g., startDate <= endDate)
            if (startDate > endDate)
            {
                ModelState.AddModelError("", "Start date must be before or equal to end date.");
                var house = _context.Houses.FirstOrDefault(h => h.Id == id);
                return View("Rent", house);
            }

            // Process the rental (save to DB, etc.)
            // Example: Save rental info, show confirmation, etc.

            return RedirectToAction("Index");
        }
    }

}
