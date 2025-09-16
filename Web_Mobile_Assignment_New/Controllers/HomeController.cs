using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QRCoder;
using Web_Mobile_Assignment_New.Models;
using ZXing.QrCode.Internal;

namespace Web_Mobile_Assignment_New.Controllers
{
    public class HomeController : Controller
    {
        private readonly DB _context;

        public HomeController(DB context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 8)
        {
            if (User.IsInRole("Admin"))
            {
                return View("Admin");
            }
            else
            {
                // 计算总数
                var totalHouses = await _context.Houses.CountAsync();

                // 拿分页数据
                var houses = await _context.Houses
                    .Include(h => h.Images)
                    .Include(h => h.Reviews)
                    .OrderBy(h => h.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 传递分页数据给 View
                ViewBag.Page = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalHouses / (double)pageSize);

                return View(houses);
            }
        }


        public async Task<IActionResult> Filter(int? minPrice, int? maxPrice, string? type)
        {
            // 从 DbContext 里先拿出 IQueryable
            var houses = _context.Houses.AsQueryable();

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHouse(House house, List<IFormFile> ImageFiles)
        {

            var UserEmail = User.Identity?.Name ?? "guest@example.com";
                house.Email = UserEmail;
            // ✅ 自动设置状态为 "Available"
            house.RoomStatus = "Valid";

            // ✅ 房间数验证
            if (house.RoomType == "Whole Unit")
            {
                if (house.Rooms < 1 || house.Rooms > 8)
                {
                    ModelState.AddModelError("Rooms", "For Whole Unit, rooms must be between 1 and 8.");
                }
            }
            else
            {
                house.Rooms = 1; // 单间固定 1
            }

            // ✅ Bathrooms 验证
            if (house.RoomType == "Whole Unit")
            {
                if (house.Bathrooms < 1 || house.Bathrooms > 6)
                {
                    ModelState.AddModelError("Bathrooms", "For Whole Unit, bathrooms must be between 1 and 6.");
                }
            }
            else
            {
                house.Bathrooms = 1;
            }

            // ✅ 租期验证
            if (!house.StartDate.HasValue || !house.EndDate.HasValue)
            {
                ModelState.AddModelError("StartDate", "Both Start Date and End Date are required.");
            }
            else
            {
                if (house.StartDate < DateTime.Today)
                {
                    ModelState.AddModelError("StartDate", "Start Date cannot be in the past.");
                }

                if (house.EndDate <= house.StartDate)
                {
                    ModelState.AddModelError("EndDate", "End Date must be later than Start Date.");
                }

                // 🔥 限制租期：Whole Unit 最长 2 年，其他最多 1 年
                var maxDuration = house.RoomType == "Whole Unit" ? 730 : 365; // 天数
                var duration = (house.EndDate.Value - house.StartDate.Value).TotalDays;

                if (duration > maxDuration)
                {
                    ModelState.AddModelError("EndDate",
                        house.RoomType == "Whole Unit"
                            ? "Whole Unit rental cannot exceed 2 years."
                            : "Rental for this room type cannot exceed 1 year.");
                }
            }

            // ✅ 至少要有一张图片
            if (ImageFiles == null || !ImageFiles.Any())
            {
                ModelState.AddModelError("ImageFiles", "At least one image is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(house);
            }

            // ✅ 保存房源（先存 House 才能拿到 Id）
            _context.Houses.Add(house);
            await _context.SaveChangesAsync();

            // ✅ 上传图片
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            bool isFirstImage = true;

            foreach (var file in ImageFiles)
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

                    // 如果只存一张图 → 也可以设置默认封面
                    if (string.IsNullOrEmpty(house.ImageUrl))
                        house.ImageUrl = "/images/" + fileName;
                }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // 房子详情 + 评论
        public async Task<IActionResult> Details(int id)
        {
            var house = await _context.Houses
                .Include(h => h.Owner)
                .Include(h => h.Images)
                .Include(h => h.Reviews)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (house == null) return NotFound();

            // Determine if current user is owner
            var userEmail = User.Identity.IsAuthenticated ? User.Identity.Name : null;
            bool isOwner = userEmail != null && userEmail == house.Email;

            ViewBag.IsOwner = isOwner;

            ViewBag.AvgRating = (house.Reviews != null && house.Reviews.Any())
                ? house.Reviews.Average(r => r.Rating)
                : 0;
            ViewBag.TotalReviews = house.Reviews?.Count ?? 0;

            return View(house);
        }

        public async Task<IActionResult> HouseList(int page = 1, int pageSize = 6)
        {
            var houses = await _context.Houses
                .OrderBy(h => h.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalHouses = await _context.Houses.CountAsync();
            var totalPages = (int)Math.Ceiling(totalHouses / (double)pageSize);

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return PartialView("_HouseListPartial", houses);
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
        public IActionResult Owner()
        {
            var UserEmail = User.Identity?.Name ?? "guest@example.com";
            var value = _context.Houses.Where(h => h.Email == UserEmail).ToList();
            return View(value);
        }
        
        [Authorize(Roles = "Admin")]
        public IActionResult Admin() => View();

        [Authorize(Roles = "Tenant")]
        public IActionResult Tenant() => View();

        // ================= RENTING ==================
        [Authorize(Roles = "Tenant")]
        public IActionResult Rent(int id)
        {
            var house = _context.Houses.FirstOrDefault(h => h.Id == id);
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

            // Check if dates are available
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

            // Create booking
            var booking = new Booking
            {
                HouseId = houseData.Id,
                UserEmail = User.Identity?.Name ?? "guest@example.com",
                StartDate = startDate,
                EndDate = endDate,
                TotalPrice = totalPrice
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Create payment record with status "Pending"
            var payment = new Payment
            {
                BookingId = booking.BookingId,
                Method = "",               // will be chosen later in Payment page
                Amount = totalPrice,
                PaymentDate = DateTime.Now,
                Status = "Pending"
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

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

                if (paymentMethod == "QRPayment")
                    existingPayment.Status = "Pending";   // pending for QR
                else
                    existingPayment.Status = "Completed"; // immediate for CreditCard
            }
            else
            {
                var payment = new Payment
                {
                    BookingId = bookingId,
                    Method = paymentMethod,
                    Amount = booking.TotalPrice,
                    PaymentDate = DateTime.Now,
                    Status = paymentMethod == "QRPayment" ? "Pending" : "Completed"
                };
                _context.Payments.Add(payment);
            }

            _context.SaveChanges();

            // ✅ Only mark house as "Rented" if ALL dates are booked and payment is completed
            if (paymentMethod != "QRPayment" && IsHouseFullyBooked(booking.HouseId))
            {
                var house = _context.Houses.FirstOrDefault(h => h.Id == booking.HouseId);
                if (house != null)
                {
                    house.RoomStatus = "Rented";
                    _context.SaveChanges();
                }
            }

            // Redirect based on payment method
            if (paymentMethod == "QRPayment")
            {
                return RedirectToAction("QRPayment", new { bookingId = bookingId });
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
        public IActionResult OwnerDetails(int id)
        {
            var house = _context.Houses
                .Include(h => h.Images) // 🔥 加这里
                .FirstOrDefault(h => h.Id == id);
            return View(house);
        }

        public IActionResult PropertyDelete(int id)
        {
            House? house = _context.Houses.FirstOrDefault(h => h.Id == id);
            if (house != null)
            {
                _context.Houses.Remove(house);
                _context.SaveChanges();
            }
            return RedirectToAction("OwnerDetails");
        }

        public IActionResult PropertyDetails(int id)
        {
            House? house = _context.Houses
                .Include(h => h.Images) // 🔥 加这里
                .FirstOrDefault(h => h.Id == id);
            if (house == null) return NotFound();
            return View(house);
        }
        public IActionResult UpdateProperty(House model)
        {
            var existing = _context.Houses.FirstOrDefault(h => h.Id == model.Id);
            if (existing == null)
            {
                TempData["Message"] = "House not found";
                return RedirectToAction("Owner");
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
                return RedirectToAction("OwnerDetails", new { id = model.Id });
            }

            _context.SaveChanges();
            TempData["Message"] = "Property Change Successful";
            return RedirectToAction("OwnerDetails", new { id = model.Id });
        }
    


        [HttpPost]
        public IActionResult CancelPayment(int bookingId)
        {
            var booking = _context.Bookings
                .Include(b => b.House)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            // Remove payment if exists
            var payment = _context.Payments.FirstOrDefault(p => p.BookingId == bookingId);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            // Remove booking
            _context.Bookings.Remove(booking);
            _context.SaveChanges();

            // Update house status if needed
            var house = booking.House;
            if (house != null)
            {
                if (IsHouseFullyBooked(house.Id))
                {
                    house.RoomStatus = "Rented";
                }
                else
                {
                    house.RoomStatus = "Available";
                }
                _context.SaveChanges();
            }

            TempData["Message"] = "Booking has been cancelled successfully.";
            return RedirectToAction("Index");
        }

        public IActionResult QRPayment(int bookingId)
        {
            var booking = _context.Bookings
                .Include(b => b.House)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            return View(booking); // QRPayment.cshtml
        }

        [HttpPost]
        public IActionResult ConfirmQRPayment([FromBody] ConfirmQRPaymentRequest request)
        {
            var payment = _context.Payments
                .FirstOrDefault(p => p.BookingId == request.BookingId && p.Status == "Pending");

            if (payment == null)
                return Json(new { success = false });

            payment.Status = "Completed";
            _context.SaveChanges();

            // Update house status if fully booked
            var booking = _context.Bookings.FirstOrDefault(b => b.BookingId == request.BookingId);
            if (booking != null && IsHouseFullyBooked(booking.HouseId))
            {
                var house = _context.Houses.FirstOrDefault(h => h.Id == booking.HouseId);
                if (house != null)
                {
                    house.RoomStatus = "Rented";
                    _context.SaveChanges();
                }
            }

            return Json(new { success = true });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReportHouse(Report report,int propretyId,string ReportType,string Details)
        {

            // 填充必需字段
            try
            {
                report.Who = User.Identity.Name; // 当前登录用户
                report.TargetProperty = propretyId;
                report.ReportType = ReportType;
                report.Details = Details;
                report.TargetEmail = null;       // 因为是举报房源
                report.CreatedAt = DateTime.UtcNow;
                report.Status = "Pending";
            }catch(Exception ex)
            {
                TempData["Message"] = "Report submitted failed!";
                return RedirectToAction("Details", new { id = report.TargetProperty });
            }
            _context.Reports.Add(report);
            _context.SaveChanges();

            TempData["Message"] = "Report submitted successfully!";
            return RedirectToAction("Details", new { id = report.TargetProperty });
        }



    }
}
