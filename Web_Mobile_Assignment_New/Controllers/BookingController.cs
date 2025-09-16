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

    // 原本的 MyBookings/OwnerBookings 不变
    public IActionResult MyBookings()
    {
        var email = User.Identity?.Name;
        var bookings = _db.Bookings
            .Include(b => b.House)
            .Where(b => b.UserEmail == email)
            .ToList();

        return View(bookings);
    }

    public IActionResult OwnerBookings()
    {
        var email = User.Identity?.Name;
        var bookings = _db.Bookings
            .Include(b => b.House)
            .Include(b => b.User)
            .Where(b => b.House.Email == email)
            .ToList();

        return View(bookings);
    }

    // ✅ 租客分页
    public IActionResult MyBookingsPage(int page = 1, int pageSize = 4)
    {
        if (page < 1) page = 1;
        var email = User.Identity?.Name;

        var query = _db.Bookings
            .Include(b => b.House)
            .Where(b => b.UserEmail == email)
            .OrderByDescending(b => b.StartDate);

        var totalItems = query.Count();
        var bookings = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return PartialView("_MyBookingsPartial", bookings);
    }

    // ✅ 房东分页
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
}
