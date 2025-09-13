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

        public IActionResult Filter(int? minPrice, int? maxPrice, string? type)
        {
            var houses = _context.Houses.AsQueryable();

            if (minPrice.HasValue)
                houses = houses.Where(h => h.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                houses = houses.Where(h => h.Price <= maxPrice.Value);

            if (!string.IsNullOrEmpty(type))
                if (type != "Whole Unit")
                    houses = houses.Where(h => h.RoomType == type);

            return View("Index", houses.ToList());
        }

        [HttpGet]
        public IActionResult AddHouse()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddHouse(House house, IFormFile ImageFile)
        {
            // ✅ 后端验证 Rooms
            if (house.RoomType == "Whole Unit")
            {
                if (house.Rooms < 1 || house.Rooms > 8)
                {
                    ModelState.AddModelError("Rooms", "For Whole Unit, rooms must be between 1 and 8.");
                }
            }
            else
            {
                house.Rooms = 1; // 非 Whole Unit 强制设为 1
            }

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

        // 🔹 房子详情 + 评论
        public IActionResult Details(int id)
        {
            var house = _context.Houses
                .Include(h => h.Reviews)
                .ThenInclude(r => r.User) // 关联用户
                .FirstOrDefault(h => h.Id == id);

            if (house == null) return NotFound();

            if (house.Reviews != null && house.Reviews.Any())
            {
                ViewBag.AvgRating = house.Reviews.Average(r => r.Rating);
                ViewBag.TotalReviews = house.Reviews.Count;
            }
            else
            {
                ViewBag.AvgRating = 0;
                ViewBag.TotalReviews = 0;
            }

            return View(house);
        }

        // 🔹 提交评论
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int houseId, int rating, string? comment)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            // 当前用户 email
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized();
            }

            var review = new HouseReview
            {
                HouseId = houseId,
                UserEmail = userEmail,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.HouseReviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = houseId });
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
            if (startDate > endDate)
            {
                ModelState.AddModelError("", "Start date must be before or equal to end date.");
                var house = _context.Houses.FirstOrDefault(h => h.Id == id);
                return View("Rent", house);
            }

            // TODO: 保存租赁信息

            return RedirectToAction("Index");
        }
    }
}
