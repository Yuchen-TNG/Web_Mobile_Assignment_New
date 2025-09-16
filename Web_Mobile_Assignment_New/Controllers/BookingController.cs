using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_Mobile_Assignment_New.Models;

[Authorize]
public class BookingController : Controller
{
    private readonly DB _db;

    public BookingController(DB db)
    {
        _db = db;
    }

    // ================= 租客页面 =================
    public IActionResult MyBookings()
    {
        return View(); // 初始视图，分页由 Ajax 加载
    }

    // 租客分页
    public IActionResult MyBookingsPage(int page = 1, int pageSize = 4)
    {
        if (page < 1) page = 1;
        var email = User.Identity?.Name;

        var query = _db.Bookings
            .Include(b => b.House)
            .Include(b => b.Payment)
            .Where(b => b.UserEmail == email)
            .OrderByDescending(b => b.StartDate);

        var totalItems = query.Count();
        var bookings = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return PartialView("_MyBookingsPartial", bookings);
    }

    // ================= 房东页面 =================
    public IActionResult OwnerBookings()
    {
        return View();
    }

    public IActionResult OwnerBookingsPage(int page = 1, int pageSize = 4)
    {
        if (page < 1) page = 1;
        var email = User.Identity?.Name;

        var query = _db.Bookings
            .Include(b => b.House)
            .Include(b => b.User)
            .Where(b => b.House.Email == email)
            .OrderByDescending(b => b.StartDate);

        var totalItems = query.Count();
        var bookings = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return PartialView("_OwnerBookingsPartial", bookings);
    }

    // ================= 取消预订 =================
    [HttpPost]
    public IActionResult CancelBooking(int id)
    {
        var booking = _db.Bookings
            .Include(b => b.House)
            .Include(b => b.Payment)
            .FirstOrDefault(b => b.BookingId == id);

        if (booking == null)
        {
            TempData["Message"] = "Booking not found.";
            TempData["MessageType"] = "error";
            return RedirectToAction("MyBookings");
        }

        // 删除关联 Payment
        if (booking.Payment != null)
        {
            _db.Payments.Remove(booking.Payment);
        }

        _db.Bookings.Remove(booking);
        _db.SaveChanges();

        // 更新房源状态
        var house = booking.House;
        if (house != null)
        {
            house.RoomStatus = IsHouseFullyBooked(house.Id) ? "Rented" : "Available";
            _db.SaveChanges();
        }

        TempData["Message"] = "Booking has been cancelled successfully.";
        TempData["MessageType"] = "success";
        return RedirectToAction("MyBookings");
    }

    // ================= Helper =================
    private bool IsHouseFullyBooked(int houseId)
    {
        // 如果还有未取消的预订，视为已被占用
        return _db.Bookings.Any(b => b.HouseId == houseId && b.EndDate >= DateTime.Today);
    }
}
