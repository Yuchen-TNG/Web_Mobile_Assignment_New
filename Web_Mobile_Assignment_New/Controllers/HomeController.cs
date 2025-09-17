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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
                // 管理员看全部
                return View("Admin");
            }
            else
            {
                // ✅ 普通用户只看可用的
                var totalHouses = await _context.Houses
                    .Where(h => h.Availability == "Available" && h.RoomStatus == "Valid")
                    .CountAsync();

                var houses = await _context.Houses
                    .Include(h => h.Images)
                    .Include(h => h.Reviews)
                    .Where(h => h.Availability == "Available" && h.RoomStatus == "Valid") // 🔑 过滤
                    .OrderBy(h => h.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.Page = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalHouses / (double)pageSize);

                return View(houses);
            }
        }

        public async Task<IActionResult> Filter(int? minPrice, int? maxPrice, string? type)
        {
            var houses = _context.Houses
                .Include(h => h.Images)
                .Include(h => h.Reviews)
                .Where(h => h.Availability == "Available" && h.RoomStatus == "Valid"); // 🔑 只取可用

            if (minPrice.HasValue)
                houses = houses.Where(h => h.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                houses = houses.Where(h => h.Price <= maxPrice.Value);

            if (!string.IsNullOrEmpty(type) && type != "All")
                houses = houses.Where(h => h.RoomType == type);

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
            house.Availability = "Available";

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

            // ✅ 🔥 在这里检查 Owner 是否存在
var owner = await _context.Owners.FirstOrDefaultAsync(o => o.Email == house.Email);
if (owner == null)
{
    ModelState.AddModelError("", "Owner not found. Please register as an Owner before adding a house.");
    return View(house);
}
house.Owner = owner;

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




        public async Task<IActionResult> AddReview(int houseId, int? rating, string comment)
        {
            if (rating == null || rating < 1 || rating > 5)
                rating = 0;

            var review = new HouseReview
            {
                HouseId = houseId,
                Rating = rating,
                Comment = comment,
                UserEmail = User.Identity?.Name ?? "guest@example.com",
                CreatedAt = DateTime.UtcNow
            };

            _context.HouseReviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = houseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteReview(int id, int houseId)
        {
            var review = _context.HouseReviews.FirstOrDefault(r => r.Id == id);
            if (review == null)
            {
                TempData["Message"] = "Review not found.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Details", "Home", new { id = houseId });
            }

            var currentUser = User.Identity?.Name;
            var isOwner = User.IsInRole("Owner");

            // 只有评论作者或者房东可以删除
            if (review.UserEmail != currentUser && !isOwner)
            {
                TempData["Message"] = "You are not allowed to delete this review.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Details", "Home", new { id = houseId });
            }

            _context.HouseReviews.Remove(review);
            _context.SaveChanges();

            TempData["Message"] = "Review deleted successfully.";
            TempData["MessageType"] = "success";
            return RedirectToAction("Details", "Home", new { id = houseId });
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

        [Authorize(Roles = "Tenant")]
        public IActionResult MyBookings()
        {
            var userEmail = User.Identity.Name;
            var bookings = _context.Bookings
                .Include(b => b.House)
                .Include(b => b.Payment)
                .Where(b => b.UserEmail == userEmail)
                .OrderByDescending(b => b.StartDate)
                .ToList();

            return View(bookings);
        }

        [Authorize(Roles = "Owner")]
        public IActionResult OwnerBookings()
        {
            var userEmail = User.Identity.Name;
            var bookings = _context.Bookings
                .Include(b => b.House)
                .Include(b => b.Payment)
                .Where(b => b.House.Email == userEmail) // 房东的 Email 存在 House.Email
                .OrderByDescending(b => b.StartDate)
                .ToList();

            return View(bookings);
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

            // ✅ Only mark Availability as "Rented" if fully booked & payment done
            if (paymentMethod != "QRPayment" && IsHouseFullyBooked(booking.HouseId))
            {
                var house = _context.Houses.FirstOrDefault(h => h.Id == booking.HouseId);
                if (house != null)
                {
                    house.Availability = "Rented";
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

            var bookedDays = new HashSet<DateTime>();
            foreach (var b in bookings)
            {
                for (var date = b.StartDate.Date; date <= b.EndDate.Date; date = date.AddDays(1))
                {
                    bookedDays.Add(date);
                }
            }

            for (var d = house.StartDate.Value.Date; d <= house.EndDate.Value.Date; d = d.AddDays(1))
            {
                if (!bookedDays.Contains(d))
                {
                    return false;
                }
            }

            return true;
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
                .Include(b => b.Payment)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            return View(booking); // PaymentSuccess.cshtml
        }
        public IActionResult OwnerDetails(int id)
        {
            var ID = _context.Houses.Where(h => h.Id == id).FirstOrDefault();
            return View(ID);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PropertyDelete(int id)
        {
            var house = _context.Houses
                .Include(h => h.Images)
                .FirstOrDefault(h => h.Id == id);

            if (house != null)
            {
                if (house.Images != null && house.Images.Any())
                {
                    _context.HouseImages.RemoveRange(house.Images);
                }

                _context.Houses.Remove(house);
                _context.SaveChanges();

                TempData["Message"] = "✅ Property deleted successfully.";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "⚠ This property does not exist.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("Index"); // ✅ 强制回到列表
        }


        public IActionResult PropertyDetails(int id)
        {
            if (id is 0) return NotFound();

            House? house = _context.Houses.FirstOrDefault(h => h.Id == id);
            if (house == null) return NotFound();

            return View(house);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProperty(House model)
        {
            var existing = _context.Houses.FirstOrDefault(h => h.Id == model.Id);
            if (existing == null)
            {
                TempData["Message"] = "House not found";
                return RedirectToAction("Owner");
            }

            // ================== 验证逻辑 ==================
            if (model.RoomType == "Whole Unit")
            {
                // Whole Unit 可以自由决定 Rooms/Bathrooms
                if (model.Rooms < 1 || model.Rooms > 8)
                    ModelState.AddModelError("Rooms", "For Whole Unit, rooms must be between 1 and 8.");
                if (model.Bathrooms < 1 || model.Bathrooms > 6)
                    ModelState.AddModelError("Bathrooms", "For Whole Unit, bathrooms must be between 1 and 6.");
            }
            else
            {
                // 非 Whole Unit 固定 1 个房间和 1 个卫生间
                model.Rooms = 1;
                model.Bathrooms = 1;
            }

            // 日期验证
            if (!model.StartDate.HasValue || !model.EndDate.HasValue)
            {
                ModelState.AddModelError("StartDate", "Both Start Date and End Date are required.");
            }
            else
            {
                if (model.StartDate < DateTime.Today)
                    ModelState.AddModelError("StartDate", "Start Date cannot be in the past.");
                if (model.EndDate <= model.StartDate)
                    ModelState.AddModelError("EndDate", "End Date must be later than Start Date.");

                // 限制租期
                var maxDuration = model.RoomType == "Whole Unit" ? 730 : 365;
                var duration = (model.EndDate.Value - model.StartDate.Value).TotalDays;
                if (duration > maxDuration)
                {
                    ModelState.AddModelError("EndDate",
                        model.RoomType == "Whole Unit"
                            ? "Whole Unit rental cannot exceed 2 years."
                            : "Rental for this room type cannot exceed 1 year.");
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();
                TempData["Message"] = string.Join("; ", errors);
                return RedirectToAction("OwnerDetails", new { id = model.Id });
            }

            // ================== 更新字段 ==================
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

            // 图片保持原样（前端有 Hidden Input）
            if (!string.IsNullOrEmpty(model.ImageUrl))
                existing.ImageUrl = model.ImageUrl;

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

            // Update house availability
            var house = booking.House;
            if (house != null)
            {
                house.Availability = IsHouseFullyBooked(house.Id) ? "Rented" : "Available";
                _context.SaveChanges();
            }

            TempData["Message"] = "Booking has been cancelled successfully.";
            return RedirectToAction("Index");
        }

        public IActionResult QRPayment(int bookingId)
        {
            var booking = _context.Bookings
                .Include(b => b.House)
                .Include(b => b.Payment)
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

            // Update house availability if fully booked
            var booking = _context.Bookings.FirstOrDefault(b => b.BookingId == request.BookingId);
            if (booking != null && IsHouseFullyBooked(booking.HouseId))
            {
                var house = _context.Houses.FirstOrDefault(h => h.Id == booking.HouseId);
                if (house != null)
                {
                    house.Availability = "Rented";
                    _context.SaveChanges();
                }
            }

            return Json(new { success = true, paymentId = payment.PaymentId });
        }

        public IActionResult Receipt(int paymentId)
        {
            var payment = _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.House)
                .FirstOrDefault(p => p.PaymentId == paymentId);

            if (payment == null) return NotFound();

            if (payment.Status != "Completed")
            {
                TempData["Message"] = "Receipt available only for successful payments.";
                return RedirectToAction("MyBookings");
            }

            var booking = payment.Booking;
            var house = booking.House;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Text("Payment Receipt")
                            .FontSize(22).Bold().FontColor(Colors.Blue.Medium)
                            .AlignCenter();

                        col.Item().Text($"Receipt ID: {payment.PaymentId}")
                            .FontSize(10).FontColor(Colors.Grey.Medium)
                            .AlignCenter();
                    });

                    // Content with table layout
                    page.Content().Column(col =>
                    {
                        col.Spacing(15);

                        col.Item().Text("Booking & Payment Details")
                            .FontSize(14).Bold().Underline();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(150); // 左边字段
                                columns.RelativeColumn();   // 右边值
                            });

                            // 🔹 Helper 方法
                            void AddRow(string label, string value)
                            {
                                table.Cell().Element(CellStyle).Text(label).Bold();
                                table.Cell().Element(CellStyle).Text(value ?? "-");
                            }

                            // 🔹 内容行
                            AddRow("Date", $"{payment.PaymentDate:yyyy-MM-dd HH:mm}");
                            AddRow("Tenant Email", booking.UserEmail);
                            AddRow("House", house.RoomName);
                            AddRow("Address", house.Address);
                            AddRow("Booking Period", $"{booking.StartDate:yyyy-MM-dd} → {booking.EndDate:yyyy-MM-dd}");
                            AddRow("Payment Method", payment.Method);
                            AddRow("Status", payment.Status);
                            AddRow("Total Paid", $"RM {payment.Amount:F2}");
                        });
                    });

                    // Footer
                    page.Footer().AlignCenter().Text("Thank you for your payment!")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });

            var pdf = document.GeneratePdf();
            return File(pdf, "application/pdf", $"Receipt_{payment.PaymentId}.pdf");
        }

        // 🔹 统一单元格样式
        static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(5);
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
