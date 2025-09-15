using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_Mobile_Assignment_New.Models;
using System.Collections.Generic;

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
                var houses = await _context.Houses
                    .Include(h => h.Images)// 加载图片
                    .Include(h => h.Reviews)

                    .ToListAsync();
                return View(houses);
            }
        }

        public async Task<IActionResult> Filter(int? minPrice, int? maxPrice, string? type)
        {
            var houses = _context.Houses.Include(h => h.ImageUrl).AsQueryable();

            if (minPrice.HasValue)
                houses = houses.Where(h => h.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                houses = houses.Where(h => h.Price <= maxPrice.Value);

            if (!string.IsNullOrEmpty(type) && type != "All")
            {
                houses = houses.Where(h => h.RoomType == type);
            }

            return View("Index", await houses.ToListAsync());
        }

        // ================= HOUSE CRUD ==================
        [HttpGet]
        public IActionResult AddHouse()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddHouse(House house, List<IFormFile> ImageFiles)
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
                _context.Houses.Add(house);
                await _context.SaveChangesAsync();

                if (ImageFiles != null && ImageFiles.Count > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    foreach (var file in ImageFiles)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(uploadsFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var houseImage = new HouseImage
                            {
                                HouseId = house.Id,
                                ImageUrl = "/images/" + fileName
                            };

                            _context.HouseImages.Add(houseImage);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Index");
            }

            return View(house);
        }

        // 房子详情 + 评论
        public async Task<IActionResult> Details(int id)
        {
            var house = await _context.Houses
                .Include(h => h.Images)   // 加载图片
                .Include(h => h.Reviews)  // 加载评论
                .FirstOrDefaultAsync(h => h.Id == id);

            if (house == null) return NotFound();

            ViewBag.AvgRating = (house.Reviews != null && house.Reviews.Any())
                ? house.Reviews.Average(r => r.Rating)
                : 0;

            ViewBag.TotalReviews = house.Reviews?.Count ?? 0;

            return View(house);
        }

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
                UserEmail = User.Identity?.Name ?? "guest@example.com", // 防止为空
                CreatedAt = DateTime.Now // 🔥 记得赋值时间
            };

            _context.HouseReviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = houseId });
        }


        [Authorize]
        public IActionResult Both() => View();

        [Authorize(Roles = "Owner")]
        public IActionResult Owner() => View();

        [Authorize(Roles = "Admin")]
        public IActionResult Admin() => View();

        [Authorize(Roles = "Tenant")]
        public IActionResult Tenant() => View();

        // ================= RENTING ==================
        public IActionResult Rent(int id)
        {
            var house = _context.Houses.FirstOrDefaultAsync(h => h.Id == id);
            if (house == null) return NotFound();

            // Pull booked ranges for this house
            var bookedRanges = _context.Bookings
                .Where(b => b.HouseId == id)
                .Select(b => new { b.StartDate, b.EndDate })
                .ToList();

            // Flatten into individual dates (so frontend can disable them)
            var bookedDates = new List<string>();
            foreach (var range in bookedRanges)
            {
                for (var date = range.StartDate.Date; date <= range.EndDate.Date; date = date.AddDays(1))
                {
                    bookedDates.Add(date.ToString("yyyy-MM-dd"));
                }
            }

            ViewBag.BookedDates = bookedDates;

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

            // ✅ Check if dates are already booked
            if (!IsDateAvailable(id, startDate, endDate))
            {
                ModelState.AddModelError("", "Selected dates are not available.");
                var house = _context.Houses.FirstOrDefault(h => h.Id == id);
                return View("Rent", house);
            }

            var houseData = _context.Houses.Find(id);
            if (houseData == null) return NotFound();

            int totalDays = (endDate - startDate).Days + 1;
            decimal totalPrice = totalDays * houseData.Price;

            var booking = new Booking
            {
                HouseId = houseData.Id,
                UserEmail = User.Identity?.Name ?? "guest@example.com",
                StartDate = startDate,
                EndDate = endDate,
                TotalPrice = totalPrice
            };

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            return RedirectToAction("Payment", new { bookingId = booking.BookingId });
        }



        public IActionResult Payment(int bookingId)
        {
            var booking = _context.Bookings
                .Include(b => b.House)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            return View(booking); // Payment.cshtml
        }

        [HttpPost]
        public IActionResult ProcessPayment(int bookingId, string paymentMethod)
        {
            var booking = _context.Bookings
                .Include(b => b.House)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            // Insert/update payment
            var existingPayment = _context.Payments.FirstOrDefault(p => p.BookingId == bookingId);
            if (existingPayment != null)
            {
                existingPayment.Method = paymentMethod;
                existingPayment.Amount = booking.TotalPrice;
                existingPayment.PaymentDate = DateTime.Now;
                existingPayment.Status = "Completed";
            }
            else
            {
                var payment = new Payment
                {
                    BookingId = bookingId,
                    Method = paymentMethod,
                    Amount = booking.TotalPrice,
                    PaymentDate = DateTime.Now,
                    Status = "Completed"
                };
                _context.Payments.Add(payment);
            }

            // ✅ Only mark house as "Rented" if ALL dates are booked
            if (IsHouseFullyBooked(booking.HouseId))
            {
                var house = _context.Houses.FirstOrDefault(h => h.Id == booking.HouseId);
                if (house != null)
                {
                    house.RoomStatus = "Rented";   // ✅ fully booked
                    _context.SaveChanges();
                }
            }
            else
            {
                var house = _context.Houses.FirstOrDefault(h => h.Id == booking.HouseId);
                if (house != null)
                {
                    house.RoomStatus = "Available"; // ✅ still has free days
                    _context.SaveChanges();
                }
            }

            return RedirectToAction("PaymentSuccess", new { bookingId = bookingId });
        }


        private bool IsHouseFullyBooked(int houseId)
        {
            var house = _context.Houses.FirstOrDefault(h => h.Id == houseId);
            if (house == null || !house.StartDate.HasValue || !house.EndDate.HasValue)
                return false;

            var bookings = _context.Bookings
                .Where(b => b.HouseId == houseId)
                .ToList();

            if (!bookings.Any()) return false;

            // Collect all booked dates
            var bookedDays = new HashSet<DateTime>();
            foreach (var b in bookings)
            {
                for (var date = b.StartDate.Date; date <= b.EndDate.Date; date = date.AddDays(1))
                {
                    bookedDays.Add(date);
                }
            }

            // Check if every day in the house's availability is covered
            for (var d = house.StartDate.Value.Date; d <= house.EndDate.Value.Date; d = d.AddDays(1))
            {
                if (!bookedDays.Contains(d))
                {
                    return false; // At least one day not booked → still Available
                }
            }

            return true; // ✅ All days booked → fully rented
        }




        private bool IsDateAvailable(int houseId, DateTime startDate, DateTime endDate)
        {
            return !_context.Bookings
                .Any(b => b.HouseId == houseId &&
                          ((startDate >= b.StartDate && startDate <= b.EndDate) ||
                           (endDate >= b.StartDate && endDate <= b.EndDate) ||
                           (startDate <= b.StartDate && endDate >= b.EndDate)));
        }


        public IActionResult PaymentSuccess(int bookingId)
        {
            var booking = _context.Bookings
                .Include(b => b.House)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            return View(booking); // PaymentSuccess.cshtml
        }
    }
}
