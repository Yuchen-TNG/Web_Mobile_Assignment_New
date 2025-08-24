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

        [HttpGet]
        public IActionResult AddHouse()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddHouse(House house)
        {
            if (ModelState.IsValid)
            {
                _context.Houses.Add(house);
                _context.SaveChanges();
                return RedirectToAction("Index"); // 添加后跳去主页面
            }
            return View(house);
        }

        public IActionResult Details(int id)
        {
            var house = _context.Houses.FirstOrDefault(h => h.Id == id);
            if (house == null) return NotFound();
            return View(house);
        }
    }
}
