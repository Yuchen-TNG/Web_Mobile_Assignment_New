﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

public class HomeController : Controller
{
    // GET: Home/Index
    public IActionResult Index()
    {
        return View();
    }

    // GET: Home/Both
    [Authorize]
    public IActionResult Both()
    {
        return View();
    }

    // GET: Home/Member
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

    // GET: Home/Member
    [Authorize(Roles = "Tenant")]
    public IActionResult Tenant()
    {
        return View();
    }
}
