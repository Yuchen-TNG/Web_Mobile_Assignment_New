using System;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                return View("Admin");
            }
            else
            {
                var houses = await _context.Houses.ToListAsync();
                return View(houses);
            }
        }

        public async Task<IActionResult> Filter(int? minPrice, int? maxPrice, string? type)
        {
            var houses = _context.Houses.AsQueryable();

            if (minPrice.HasValue)
                houses = houses.Where(h => h.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                houses = houses.Where(h => h.Price <= maxPrice.Value);

            if (!string.IsNullOrEmpty(type) && type != "Whole Unit")
                houses = houses.Where(h => h.RoomType == type);

            return View("Index", await houses.ToListAsync());
        }

        [HttpGet]
        public IActionResult AddHouse()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddHouse(House house, IFormFile ImageFile)
        {
            if (house.RoomType == "Whole Unit")
            {
                if (house.Rooms < 1 || house.Rooms > 8)
                {
                    ModelState.AddModelError("Rooms", "For Whole Unit, rooms must be between 1 and 8.");
                }
            }
            else
            {
                house.Rooms = 1;
            }

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    house.ImageUrl = "/images/" + fileName;
                }

                _context.Houses.Add(house);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(house);
        }

        // 房子详情 + 评论
        public async Task<IActionResult> Details(int id)
        {
            var house = await _context.Houses
                .Include(h => h.Reviews) // 加载评论
                .FirstOrDefaultAsync(h => h.Id == id);

            if (house == null) return NotFound();

            ViewBag.AvgRating = (house.Reviews != null && house.Reviews.Any())
                ? house.Reviews.Average(r => r.Rating)
                : 0;

            ViewBag.TotalReviews = house.Reviews?.Count ?? 0;

            return View(house);
        }

        // 提交评论
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int houseId, int rating, string comment)
        {
            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Invalid rating value.";
                return RedirectToAction("Details", new { id = houseId });
            }

            var review = new HouseReview
            {
                HouseId = houseId,
                Rating = rating,
                Comment = comment,
                UserEmail = User.Identity?.Name
            };

            _context.HouseReviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = houseId });
        }

        [Authorize]
        public IActionResult Both()
        {
            return View();
        }

        [Authorize(Roles = "Owner")]
        public IActionResult Owner()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            return View();
        }

        [Authorize(Roles = "Tenant")]
        public IActionResult Tenant()
        {
            return View();
        }

        public async Task<IActionResult> Rent(int id)
        {
            var house = await _context.Houses.FirstOrDefaultAsync(h => h.Id == id);
            if (house == null) return NotFound();
            return View(house);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmRent(int id, DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                ModelState.AddModelError("", "Start date must be before or equal to end date.");
                var house = await _context.Houses.FirstOrDefaultAsync(h => h.Id == id);
                return View("Rent", house);
            }

            // TODO: 保存租赁信息

            return RedirectToAction("Index");
        }
    }
}
