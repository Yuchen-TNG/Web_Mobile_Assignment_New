using Web_Mobile_Assignment_New.Modelss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_Mobile_Assignment_New.Models;
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

        public async Task<IActionResult> UserFilter(string? role, string? status, DateOnly? Birtday)
        {
            var housesQuery = _context.Users.OfType<OwnerTenant>().AsQueryable();

            if (!string.IsNullOrEmpty(role))
                housesQuery = housesQuery.Where(h => h.Role == role);

            if (!string.IsNullOrEmpty(status))
                housesQuery = housesQuery.Where(h => h.Status == status);

            if (Birtday.HasValue)
                housesQuery = housesQuery.Where(h => h.Birthday == Birtday.Value);

            var houses = await housesQuery.ToListAsync();

            return View(houses); // 或 return View(houses);
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
            if (id == 0) return NotFound();

            // ✅ Include Images
            House? house = _context.Houses
                                   .Include(h => h.Images)
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
            var house = _context.Houses.Include(h => h.Images).FirstOrDefault(h => h.Id == houseId);
            if (house == null) return NotFound();

            if (photos != null && photos.Count > 0)
            {
                foreach (var photo in photos)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(photo.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(stream);
                    }

                    house.Images.Add(new HouseImage { ImageUrl = fileName });
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = "Images uploaded successfully!";
            }
            else
            {
                TempData["Message"] = "No images selected!";
            }

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
