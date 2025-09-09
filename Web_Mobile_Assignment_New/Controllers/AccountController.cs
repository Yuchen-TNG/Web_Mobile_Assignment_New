using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mail;

namespace Web_Mobile_Assignment_New.Controllers;

public class AccountController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public AccountController(DB db,IWebHostEnvironment en, Helper hp)
    {
        this.db = db;
        this.en = en;
        this.hp = hp;
    }

    // GET: Account/Login
    public IActionResult Login(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnUrl)
    {
        var u = db.Users.Find(vm.Email);
        var s = db.Users.Find(vm.Status);

        if (u == null || !hp.VerifyPassword(u.Hash, vm.Password))
        {
            ModelState.AddModelError("", "Login credentials not matched.");
        }

        if (ModelState.IsValid)
        {
            if (u is OwnerTenant OT && OT.Status == "restricted")
            {
                ModelState.AddModelError("", "This account is restricted.");
                return View(vm);
            }

            TempData["Info"] = "Login successfully.";
            hp.SignIn(u!.Email, u.Role, vm.RememberMe);

            // Redirect to returnUrl if valid, otherwise to Home/Index
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        ViewData["ReturnUrl"] = returnUrl;
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
                    PhotoURL = hp.SavePhoto(vm.Photo, "Photos"),
                    Birthday = vm.Birthday,
                };
            }
            else // Tenant
            {
                user = new Tenant()
                {
                    Email = vm.Email,
                    Hash = hp.HashPassword(vm.Password),
                    Name = vm.Name,
                    PhotoURL = hp.SavePhoto(vm.Photo, "Photos"),
                    Birthday = vm.Birthday,
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
                Birthday = m.Birthday,
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
                Birthday = m.Birthday,
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
            user.Name = vm.Name;
            user.Birthday = vm.Birthday;

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
        vm.Birthday = user.Birthday;
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
            // 生成验证码
            string verificationCode = hp.GenerateVerificationCode();

            // 存储验证码到Session
            hp.SetVerificationCode(vm.Email, verificationCode);

            // 发送验证码邮件
            hp.SendVerificationCodeEmail(u!, verificationCode);

            TempData["Info"] = $"Verification code has been sent to your email. Please check your email.";

            // 重定向到验证码输入页面
            return RedirectToAction("VerifyCode", "Account", new { email = vm.Email });

        }

        return View(vm);
    }


    // GET: Account/VerifyCode
    [HttpGet("Account/VerifyCode/{email?}")]
    public IActionResult VerifyCode(string email)
    {

        if (string.IsNullOrEmpty(email))
        {
            TempData["info"] = "Email parameter is missing.";
            return RedirectToAction("ResetPassword");
        }

        var vm = new VerifyCodeVM { Email = email };

        // 从 Session 取到过期时间
        var expiryString = HttpContext.Session.GetString($"VerificationCodeExpiry_{email}");
        if (DateTime.TryParse(expiryString, out var expiry))
        {
            var remainingSeconds = (int)(expiry - DateTime.Now).TotalSeconds;
            vm.SecondsLeft = remainingSeconds > 0 ? remainingSeconds : 0;
        }
        else
        {
            vm.SecondsLeft = 0;
        }

        return View(vm);
    }

    // POST: Account/VerifyCode - 第二步：验证验证码
    [HttpPost]
    public IActionResult VerifyCode(VerifyCodeVM vm)
    {
        // 添加调试日志
        System.Diagnostics.Debug.WriteLine($"VerifyCode POST - Email: {vm.Email}, Code: {vm.VerificationCode}");

        if (string.IsNullOrEmpty(vm.Email))
        {
            ModelState.AddModelError("", "Email is required.");
            vm.SecondsLeft = hp.GetVerificationSecondsLeft(vm.Email ?? "");
            return View(vm);
        }

        if (ModelState.IsValid)
        {
            // 验证验证码
            if (!hp.VerifyCode(vm.Email, vm.VerificationCode))
            {
                vm.SecondsLeft = hp.GetVerificationSecondsLeft(vm.Email);
                ModelState.AddModelError("VerificationCode", "Invalid or expired verification code.");
                return View(vm);
            }

            // 验证码正确，查找用户并重置密码
            var u = db.Users.Find(vm.Email);
            if (u == null)
            {
                vm.SecondsLeft = hp.GetVerificationSecondsLeft(vm.Email);
                ModelState.AddModelError("", "User not found.");
                return View(vm);
            }

            // 生成新密码
            string newPassword = hp.RandomPassword();

            // 更新用户密码
            u.Hash = hp.HashPassword(newPassword);
            db.SaveChanges();

            // 发送新密码邮件
            SendResetPasswordEmail(u, newPassword);

            TempData["Info"] = "Password reset successfully. Check your email for the new password.";
            return RedirectToAction("Login");
        }

        vm.SecondsLeft = hp.GetVerificationSecondsLeft(vm.Email ?? "");
        return View(vm);
    }

    // GET: Account/ResendCode - 重新发送验证码
    public IActionResult ResendCode(string emails)
    {
        if (string.IsNullOrEmpty(emails))
        {
            return RedirectToAction("ResetPassword");
        }

        var u = db.Users.Find(emails);
        if (u == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("ResetPassword");
        }

        // 生成新的验证码
        string verificationCode = hp.GenerateVerificationCode();

        // 存储验证码到Session
        hp.SetVerificationCode(emails, verificationCode);

        // 发送验证码邮件
        hp.SendVerificationCodeEmail(u, verificationCode);

        TempData["Info"] = "New verification code has been sent to your email.";

        return RedirectToAction("VerifyCode", new { email = emails });
    }

    private void SendResetPasswordEmail(User u, string password)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Reset Password";
        mail.IsBodyHtml = true;

        var url = Url.Action("Login", "Account", null, "https");

        var path = u switch
        {
            Admin => Path.Combine(en.WebRootPath, "photos", "admin.jpg"),
            Tenant T => Path.Combine(en.WebRootPath, "photos", T.PhotoURL),
            Owner O => Path.Combine(en.WebRootPath, "photos", O.PhotoURL),
            _ => "",
        };

        var att = new Attachment(path);
        mail.Attachments.Add(att);
        att.ContentId = "photo";

        mail.Body = $@"
            <img src='cid:photo' style='width: 200px; height: 200px;
                                        border: 1px solid #333'>
            <p>Dear {u.Name},<p>
            <p>Your password has been reset to:</p>
            <h1 style='color: red'>{password}</h1>
            <p>
                Please <a href='{url}'>login</a>
                with your new password.
            </p>
            <p>From, 🐱 Rental Management</p>
        ";

        hp.SendEmail(mail);
    }

}
