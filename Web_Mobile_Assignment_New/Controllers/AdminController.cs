using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using Web_Mobile_Assignment_New.Models;
using Web_Mobile_Assignment_New.Modelss;

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
        public IActionResult BookingManagement()
        {
            var vm = new BookingManagementVM
            {
                Bookings = _context.Bookings
                                   .Include(b => b.House)
                                   .Include(b => b.User)
                                   .ToList(),
                Houses = _context.Houses.ToList()
            };
            return View(vm);
        }

        public IActionResult BookingDetails(int id)
        {
            var booking = _context.Bookings
                                  .Include(b => b.House)
                                  .Include(b => b.User)
                                  .Include(b => b.Payment)
                                  .FirstOrDefault(b => b.BookingId == id);

            if (booking == null)
            {
                TempData["Message"] = "Booking not found!";
                TempData["MessageType"] = "error";
                return RedirectToAction("BookingManagement");
            }

            return View(booking);
        }
        public async Task<IActionResult> UserFilter(string? role, string? status, DateOnly? birthday)
        {
            // 1️⃣ 从数据库获取所有用户（User 表）  
            var allUsers = await _context.Users.ToListAsync();

            // 2️⃣ 只筛选 OwnerTenant 子类（Owner/Tenant）
            var ownerTenantUsers = allUsers
                .Where(u => u is OwnerTenant)
                .Cast<OwnerTenant>();

            // 3️⃣ 根据 role 过滤
            if (!string.IsNullOrEmpty(role))
            {
                ownerTenantUsers = role switch
                {
                    "Owner" => ownerTenantUsers.Where(u => u is Owner),
                    "Tenant" => ownerTenantUsers.Where(u => u is Tenant),
                    _ => ownerTenantUsers
                };
            }

            // 4️⃣ 根据 status 过滤
            if (!string.IsNullOrEmpty(status))
            {
                ownerTenantUsers = ownerTenantUsers.Where(u => u.Status == status);
            }

            // 5️⃣ 根据 birthday 过滤
            if (birthday.HasValue)
            {
                ownerTenantUsers = ownerTenantUsers.Where(u => u.Birthday == birthday.Value);
            }

            // 6️⃣ 转为 List<User> 类型传给视图，保证 Razor 视图类型安全
            var usersToView = ownerTenantUsers.Cast<User>().ToList();

            return View("UserManagement", usersToView);
        }



        [HttpPost]
        public async Task<IActionResult> UserUploadPhoto(IFormFile photoFile, string email)
        {
            if (photoFile == null || photoFile.Length == 0)
            {
                TempData["Message"] = "No file selected!";
                TempData["MessageType"] = "error";
                return RedirectToAction("UserDetails", new { email = email });
            }

            var user = _context.Users
                               .AsEnumerable()          // ⚠️ 会把所有 User 拉到内存
                               .OfType<OwnerTenant>()
                               .FirstOrDefault(u => u.Email == email);

            if (user == null) return NotFound();

            // 生成唯一文件名
            var fileName = Guid.NewGuid() + Path.GetExtension(photoFile.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/photos", fileName);

            // 保存文件
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photoFile.CopyToAsync(stream);
            }

            // 如果之前有旧照片，尝试删除
            if (!string.IsNullOrEmpty(user.PhotoURL))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/photos", user.PhotoURL);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            // 更新数据库
            user.PhotoURL = fileName;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Photo updated successfully!";
            TempData["MessageType"] = "success";

            return RedirectToAction("UserDetails", new { email = email });
        }
        public IActionResult PropertyManagement()
        {
            var houses = _context.Houses
                .Include(h => h.Images)   // 🔑 加上这句
                .ToList();

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
            var house = _context.Houses
                .Include(h => h.Images)   // 必须 Include
                .FirstOrDefault(h => h.Id == id);

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
            if (user is OwnerTenant otUser)
            {
                otUser.PhotoURL = null;
                _context.Users.Update(otUser);
                _context.SaveChanges();
                TempData["Message"] = "Delete Successful";
            }
            else
            {
                TempData["Message"] = "Delete failed";
            }

            return RedirectToAction("UserDetails", new { email = email });
        }

        [HttpPost]
        public IActionResult DeleteUser(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Message"] = "❌ No email provided.";
                return RedirectToAction("UserManagement");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                TempData["Message"] = "❌ User not found.";
                return RedirectToAction("UserManagement");
            }

            try
            {
                // -------------------------------
                // 1. 删除用户相关的 HouseReviews
                // -------------------------------
                var reviews = _context.HouseReviews.Where(r => r.UserEmail == email).ToList();
                if (reviews.Any())
                    _context.HouseReviews.RemoveRange(reviews);

                // -------------------------------
                // 2. 删除用户相关的 Bookings
                // -------------------------------
                var bookings = _context.Bookings.Where(b => b.UserEmail == email).ToList();
                if (bookings.Any())
                    _context.Bookings.RemoveRange(bookings);

                // -------------------------------
                // 3. 删除用户相关的 Reports
                // -------------------------------
                var reports = _context.Reports.Where(r => r.TargetEmail == email).ToList();
                if (reports.Any())
                    _context.Reports.RemoveRange(reports);

                // -------------------------------
                // 4. 如果用户是房东 (Owner)，删除房屋及其相关数据
                // -------------------------------
                var houses = _context.Houses.Where(h => h.Email == email).ToList();
                foreach (var house in houses)
                {
                    // 删除房屋相关的 HouseReviews
                    var houseReviews = _context.HouseReviews.Where(r => r.HouseId == house.Id).ToList();
                    if (houseReviews.Any())
                        _context.HouseReviews.RemoveRange(houseReviews);

                    // 删除房屋相关的 Bookings
                    var houseBookings = _context.Bookings.Where(b => b.HouseId == house.Id).ToList();
                    if (houseBookings.Any())
                        _context.Bookings.RemoveRange(houseBookings);

                    // 删除房屋相关的 HouseImages
                    var houseImages = _context.HouseImages.Where(img => img.HouseId == house.Id).ToList();
                    if (houseImages.Any())
                        _context.HouseImages.RemoveRange(houseImages);

                    // 删除房屋本身
                    _context.Houses.Remove(house);
                }

                // -------------------------------
                // 5. 删除用户本身
                // -------------------------------
                _context.Users.Remove(user);

                // -------------------------------
                // 6. 保存数据库更改
                // -------------------------------
                _context.SaveChanges();

                TempData["Message"] = "✅ User and all related data deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "❌ Delete failed: " + ex.Message;
            }

            return RedirectToAction("UserManagement");
        }


        public IActionResult RestrictedUser(string? email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user is OwnerTenant otUser)
            {
                otUser.Status = "restricted";
                _context.SaveChanges();
                TempData["Message"] = "User has been restricted";
            }
            else
            {
                TempData["Message"] = "System problem";
            }
            return RedirectToAction("UserDetails", new { email = email });

        }


        public IActionResult ValidUser(string? email) {  
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user is OwnerTenant otUser)
            {
                otUser.Status = "valid";
                _context.SaveChanges();
                TempData["Message"] = "User has been valid";
            }
            else
            {
                TempData["Message"] = "System problem";
            }
            return RedirectToAction("UserDetails", new { email = email });

        }
        [HttpPost]
        public IActionResult UpdateProperty(House model)
        {
            if (!ModelState.IsValid)
            {
                // 返回原页面并保留用户输入
                return View(model);
            }
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


        [HttpPost]
        public IActionResult PropertyDelete(int id)
        {
            try
            {
                var house = _context.Houses.FirstOrDefault(h => h.Id == id);
                if (house != null)
                {
                    _context.Houses.Remove(house);
                    _context.SaveChanges();
                    TempData["Message"] = "Deleted Successful.";
                }
                else
                {
                    TempData["Message"] = "Deleted Failed, no related house.";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Delete failed: " + ex.Message;
            }

            return RedirectToAction("PropertyManagement");
        }


        public IActionResult RestrictedProperty(int id)
        {
            var house = _context.Houses.FirstOrDefault(p => p.Id == id);
            if (house != null)
            {
                house.RoomStatus = "restricted";
                _context.SaveChanges();
                TempData["Message"] = "Proprety has been change to restricted";
            }
            else
                TempData["Message"] = "System problem";
            return RedirectToAction("PropertyDetails", new { id = id });
                    
        }

        public IActionResult ValidProperty(int id)
        {
            var house = _context.Houses.FirstOrDefault(p => p.Id == id);
            if (house != null)
            {
                house.RoomStatus = "valid";
                _context.SaveChanges();
                TempData["Message"] = "Proprety has been change to valid";
            }
            else
                TempData["Message"] = "System problem";
            return RedirectToAction("PropertyDetails", new { id = id });

        }
        public IActionResult ReportManagement()
        {
            var rp = _context.Reports.ToList();
                return View(rp);
        }
        public IActionResult PropertyReport()
        {
            var rp = _context.Reports.Where(r => r.TargetProperty != null).ToList();
            return View("ReportManagement",rp);
        }
        public IActionResult UserReport()
        {   
            var rp = _context.Reports.Where(r => r.TargetEmail != null).ToList();
            return View("ReportManagement",rp);
        }


        [HttpPost]
        public IActionResult RotatePhoto(string email, string? direction, int? degrees)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == email);

                if (user is OwnerTenant ot && !string.IsNullOrEmpty(ot.PhotoURL))
                {
                    // 去掉缓存参数 ?v=xxx
                    var cleanUrl = ot.PhotoURL.Split('?')[0];
                    var photoFileName = Path.GetFileName(cleanUrl);
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/photos",
                        photoFileName
                    );

                    if (!System.IO.File.Exists(filePath))
                    {
                        TempData["Message"] = "❌ File not found: " + filePath;
                        TempData["MessageType"] = "error";
                        return RedirectToAction("UserDetails", new { email });
                    }

                    using (var image = SixLabors.ImageSharp.Image.Load(filePath))
                    {
                        if (!string.IsNullOrEmpty(direction))
                        {
                            if (direction == "left")
                                image.Mutate(x => x.Rotate(-90));
                            else if (direction == "right")
                                image.Mutate(x => x.Rotate(90));
                        }
                        else if (degrees.HasValue)
                        {
                            image.Mutate(x => x.Rotate(degrees.Value));
                        }

                        image.Save(filePath);
                    }

                    // ✅ 只替换缓存参数，不要重复拼接 /photos/
                    ot.PhotoURL = cleanUrl + "?v=" + DateTime.Now.Ticks;
                    _context.SaveChanges();

                    TempData["Message"] = "✅ Photo rotated successfully.";
                    TempData["MessageType"] = "success";
                }
                else
                {
                    TempData["Message"] = "❌ User has no photo.";
                    TempData["MessageType"] = "error";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "❌ Rotate failed: " + ex.Message;
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("UserDetails", new { email });
        }




        public IActionResult ReportDetails(int id)
        {
            var report = _context.Reports.FirstOrDefault(r => r.Id == id);
            if (report == null) return NotFound();

            House? house = null;
            if (report.TargetProperty.HasValue)
            {
                house = _context.Houses.FirstOrDefault(h => h.Id == report.TargetProperty.Value);
            }

            var vm = new ReportHouseViewModel
            {
                Reports = report,
                Houses = house
            };

            return View(vm);
        }

        public IActionResult ValidReport(int reportId, int houseId)
        {
            var report = _context.Reports.FirstOrDefault(r => r.Id == reportId);
            if (report == null) return NotFound();

            _context.Reports.Remove(report);   // ✅ 删除找到的对象
            _context.SaveChanges();
            var house = _context.Houses.FirstOrDefault(r => r.Id == houseId);
            if (house == null) return NotFound();

            house.RoomStatus = "Valid";
            _context.SaveChanges();

            TempData["Message"] = "Report deleted (marked as Valid).";
            return RedirectToAction("ReportManagement"); // 删除后返回列表
        }


        public IActionResult RestrictedReport(int reportId,int houseId)
        {
            var report = _context.Reports.FirstOrDefault(r => r.Id == reportId);
            if (report == null) return NotFound();

            _context.Reports.Remove(report);   // ✅ 删除找到的对象
            _context.SaveChanges();
            var house = _context.Houses.FirstOrDefault(r => r.Id == houseId);
            if (house == null) return NotFound();

            house.RoomStatus= "Restricted";
            _context.SaveChanges();

            TempData["Message"] = "Report marked as Restricted.";
            return RedirectToAction("ReportManagement");
        }

        [HttpPost]
        public async Task<IActionResult> UploadHouseImages(int houseId, List<IFormFile> photos)
        {
            try
            {
                // 🔎 找房源，带上 Images
                var house = _context.Houses
                    .Include(h => h.Images)
                    .FirstOrDefault(h => h.Id == houseId);

                if (house == null)
                {
                    TempData["Message"] = "❌ House not found!";
                    TempData["MessageType"] = "error";
                    return RedirectToAction("PropertyDetails", new { id = houseId });
                }

                if (photos != null && photos.Count > 0)
                {
                    foreach (var photo in photos)
                    {
                        if (photo.Length <= 0) continue;

                        // 生成唯一文件名
                        var fileName = Guid.NewGuid() + Path.GetExtension(photo.FileName);

                        // wwwroot/images 目录
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        var filePath = Path.Combine(uploadPath, fileName);

                        // 写入物理文件
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await photo.CopyToAsync(stream);
                        }

                        // 存数据库时带 /images/ 前缀，方便 <img src="...">
                        house.Images.Add(new HouseImage
                        {
                            ImageUrl = "/images/" + fileName
                        });
                    }

                    await _context.SaveChangesAsync();
                    TempData["Message"] = "✅ Images uploaded successfully!";
                    TempData["MessageType"] = "success";
                }
                else
                {
                    TempData["Message"] = "⚠ No images selected!";
                    TempData["MessageType"] = "warning";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "❌ Upload failed: " + ex.Message;
                TempData["MessageType"] = "error";
            }

            // 上传后回到详情页
            return RedirectToAction("PropertyDetails", new { id = houseId });
        }

        [HttpPost]
        public IActionResult DeleteHouseImage([FromBody] DeleteHouseImageRequest req)
        {
            var image = _context.HouseImages.FirstOrDefault(i => i.Id == req.ImageId);
            if (image != null)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", image.ImageUrl);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                _context.HouseImages.Remove(image);
                _context.SaveChanges();
                TempData["Message"] = "Image deleted successfully!";
                return Ok(new { success = true });
            }

            return BadRequest(new { success = false });
        }

        public class DeleteHouseImageRequest
        {
            public int ImageId { get; set; }
            public int HouseId { get; set; }
        }


    }
}
