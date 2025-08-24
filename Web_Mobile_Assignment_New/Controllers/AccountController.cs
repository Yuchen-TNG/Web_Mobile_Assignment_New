using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web_Mobile_Assignment_New.Controllers;

public class AccountController : Controller
{
    private readonly DB db;
    private readonly Helper hp;

    public AccountController(DB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }

    // GET: Account/Login
    public IActionResult Login()
    {
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        // (1) Get user (admin or member) record based on email (PK)
        var u = db.Users.Find(vm.Email);

        // (2) Custom validation -> verify password
        if (u == null || !hp.VerifyPassword(u.Hash, vm.Password))
        {
            ModelState.AddModelError("", "Login credentials not matched.");
        }

        if (ModelState.IsValid)
        {
            TempData["Info"] = "Login successfully.";

            // (3) Sign in
            hp.SignIn(u!.Email, u.Role, vm.RememberMe);

            // (4) Handle return URL
            if(string.IsNullOrEmpty(returnURL))
            {
                return RedirectToAction("Index", "Home");
            }
        }
        
        return View(vm);
    }

    // GET: Account/Logout
    public IActionResult Logout(string? returnURL)
    {
        TempData["Info"] = "Logout successfully.";

        // Sign out
        hp.SignOut();

        return RedirectToAction("Index", "Home");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View("AccessDenied", "Home");
    }



    // ------------------------------------------------------------------------
    // Others
    // ------------------------------------------------------------------------

    // GET: Account/CheckEmail
    public bool CheckEmail(string email)
    {
        return !db.Users.Any(u => u.Email == email);
    }

    // GET: Account/Register
    public IActionResult Register()
    {
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        if (ModelState.IsValid("Email") &&
            db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        if (ModelState.IsValid("Photo"))
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            User user;

            if (vm.Category == "Owner")
            {
                user = new Owner()
                {           
                    Email = vm.Email,
                    Hash = hp.HashPassword(vm.Password),
                    Name = vm.Name,
                    PhotoURL = hp.SavePhoto(vm.Photo, "Photos")
                };
            }
            else // Tenant
            {
                user = new Tenant()
                {
                    Email = vm.Email,
                    Hash = hp.HashPassword(vm.Password),
                    Name = vm.Name,
                    PhotoURL = hp.SavePhoto(vm.Photo, "Photos")
                };
            }

            db.Users.Add(user);
            db.SaveChanges();

            TempData["Info"] = "Register successfully. Please login.";
            return RedirectToAction("Login");
        }

        return View(vm);
    }

    // GET: Account/UpdatePassword
    [Authorize]
    public IActionResult UpdatePassword()
    {
        return View();
    }

    // POST: Account/UpdatePassword
    [Authorize]
    [HttpPost]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        // Get user (admin or member) record based on email (PK)
        var u = db.Users.Find(User.Identity!.Name);
        if (u == null) return RedirectToAction("Index", "Home");

        // If current password not matched
        if (!hp.VerifyPassword(u.Hash, vm.Current))
        {
            ModelState.AddModelError("Current", "Current Password not matched.");
        }

        if (ModelState.IsValid)
        {
            // Update user password (hash)
            u.Hash = hp.HashPassword(vm.New);
            db.SaveChanges();

            TempData["Info"] = "Password updated.";
            return RedirectToAction();
        }

        return View();
    }

    // GET: Account/UpdateProfile
    [Authorize(Roles = "Owner, Tenant")]
    public IActionResult UpdateProfile()
    {

        if (User.IsInRole("Owner"))
        {
            // Get Owner record based on email (PK)
            var m = db.Owners.Find(User.Identity!.Name);
            if (m == null) return RedirectToAction("Index", "Home");

            var vm = new UpdateProfileVM
            {
                Email = m.Email,
                Name = m.Name,
                PhotoURL = m.PhotoURL,
            };

            return View(vm);
        }
        else if (User.IsInRole("Tenant"))
        {
            // Get Tenant record based on email (PK)
            var m = db.Tenants.Find(User.Identity!.Name);
            if (m == null) return RedirectToAction("Index", "Home");

            var vm = new UpdateProfileVM
            {
                Email = m.Email,
                Name = m.Name,
                PhotoURL = m.PhotoURL,
            };
            return View(vm);
        }

        // fallback
        return RedirectToAction("Index", "Home");
    }

#nullable disable warnings
    // POST: Account/UpdateProfile
    [Authorize(Roles = "Owner, Tenant")]
    [HttpPost]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        object m = null;

        // Get Owner or Tenant record based on email (PK)
        if (User.IsInRole("Owner"))
            m = db.Owners.Find(User.Identity!.Name);
        else if (User.IsInRole("Tenant"))
            m = db.Tenants.Find(User.Identity!.Name);

        if (m == null) return RedirectToAction("Index", "Home");

        dynamic user = m; // allows accessing Email, Name, PhotoURL

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            var Name = vm.Name;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(user.PhotoURL, "photos");
                user.PhotoURL = hp.SavePhoto(vm.Photo, "photos");
            }

            db.SaveChanges();

            TempData["Info"] = "Profile updated.";
            return RedirectToAction();
        }

        vm.Email = user.Email;
        vm.PhotoURL = user.PhotoURL;
        return View(vm);
    }

    // GET: Account/ResetPassword
    public IActionResult ResetPassword()
    {
        return View();
    }

    // POST: Account/ResetPassword
    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        var u = db.Users.Find(vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
        }

        if (ModelState.IsValid)
        {
            // Generate random password
            string password = hp.RandomPassword();

            // Update user (admin or member) record
            u!.Hash = hp.HashPassword(password);
            db.SaveChanges();

            // Send reset password email
            TempData["Info"] = $"Password reset to <b>{password}</b>.";
            return RedirectToAction();
        }

        return View();
    }
}
