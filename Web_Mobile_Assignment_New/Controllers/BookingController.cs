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

    // 租客查看自己的预订历史
    public IActionResult MyBookings()
    {
        var email = User.Identity?.Name; // 登录用户的 Email

        var bookings = _db.Bookings
            .Include(b => b.House)
            .Where(b => b.UserEmail == email)
            .ToList();

        return View(bookings);
    }

    // 房东查看自己的屋子被谁租过
    public IActionResult OwnerBookings()
    {
        var email = User.Identity?.Name; // 当前房东 Email

        var bookings = _db.Bookings
            .Include(b => b.House)
            .Include(b => b.User)
            .Where(b => b.House.Email == email) // House.Email = Owner.Email
            .ToList();

        return View(bookings);
    }

    // 租客创建 Booking
    [HttpPost]
    public IActionResult Create(int houseId, DateTime startDate, DateTime endDate)
    {
        var email = User.Identity?.Name;

        var house = _db.Houses.Find(houseId);
        if (house == null) return NotFound();

        var days = (endDate - startDate).Days;
        if (days <= 0) return BadRequest("Invalid dates");

        var booking = new Booking
        {
            HouseId = houseId,
            UserEmail = email,
            StartDate = startDate,
            EndDate = endDate,
            TotalPrice = house.Price * days
        };

        _db.Bookings.Add(booking);
        _db.SaveChanges();

        return RedirectToAction("MyBookings");
    }
}
