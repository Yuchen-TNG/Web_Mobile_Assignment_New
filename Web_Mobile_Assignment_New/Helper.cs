using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Web_Mobile_Assignment_New;

public class Helper
{
    private readonly IWebHostEnvironment en;
    private readonly IHttpContextAccessor ct;
    private readonly IConfiguration cf;

    public Helper(IWebHostEnvironment en, IHttpContextAccessor ct, IConfiguration cf)
    {
        this.en = en;
        this.ct = ct;
        this.cf = cf;
    }

    // ------------------------------------------------------------------------
    // Photo Upload
    // ------------------------------------------------------------------------

    public string ValidatePhoto(IFormFile f)
    {
        var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG and PNG photo is allowed.";
        }
        else if (f.Length > 1 * 1024 * 1024)
        {
            return "Photo size cannot more than 1MB.";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(en.WebRootPath, folder, file);

        var options = new ResizeOptions
        {
            Size = new(200, 200),
            Mode = ResizeMode.Crop,
        };

        using var stream = f.OpenReadStream();
        using var img = Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.Save(path);

        return file;
    }

    public void DeletePhoto(string file, string folder)
    {
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, folder, file);
        File.Delete(path);
    }



    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------

    
    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password) 
            == PasswordVerificationResult.Success;
    }

    public void SignIn(string email, string role, bool rememberMe)
    {
        // (1) Claim, identity and principal
        List<Claim> claims = 
            [
                  new(ClaimTypes.Name, email),
                  new(ClaimTypes.Role, role),
            ];


        ClaimsIdentity identity = new(claims, "Cookies");

        ClaimsPrincipal principal = new(identity);

        // (2) Remember me (authentication properties)
        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
        };

        // (3) Sign in
        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void SignOut()
    {
        // Sign out
        ct.HttpContext!.SignOutAsync();
    }

    public string RandomPassword()
    {
        string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string password = "";

        Random r = new();

        for (int i = 1; i <= 10; i++)
        {
            password += s[r.Next(s.Length)];
        }

        return password;
    }

    public void SendEmail(MailMessage mail)
    {

        string user = cf["Smtp:User"] ?? "";
        string pass = cf["Smtp:Pass"] ?? "";
        string name = cf["Smtp:Name"] ?? "";
        string host = cf["Smtp:Host"] ?? "";
        int port = cf.GetValue<int>("Smtp:Port");

        mail.From = new MailAddress(user, name);

        using var smtp = new SmtpClient
        {
            Host = host,
            Port = port,
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass),
        };

        smtp.Send(mail);
    }

    // ------------------------------------------------------------------------
    // 验证码相关功能
    // ------------------------------------------------------------------------

    /// <summary>
    /// 生成6位数字验证码
    /// </summary>
    public string GenerateVerificationCode()
    {
        Random random = new();
        return random.Next(100000, 999999).ToString();
    }

    /// <summary>
    /// 设置验证码到Session（包含过期时间）
    /// </summary>
    public void SetVerificationCode(string email, string code)
    {
        var session = ct.HttpContext!.Session;
        session.SetString($"VerificationCode_{email}", code);
        session.SetString($"VerificationCodeExpiry_{email}", DateTime.Now.AddMinutes(5).ToString("yyyy-MM-dd HH:mm:ss"));
    }

    /// <summary>
    /// 验证验证码
    /// </summary>
    public bool VerifyCode(string email, string inputCode)
    {
        var session = ct.HttpContext!.Session;
        var storedCode = session.GetString($"VerificationCode_{email}");
        var expiryString = session.GetString($"VerificationCodeExpiry_{email}");

        if (string.IsNullOrEmpty(storedCode) || string.IsNullOrEmpty(expiryString))
        {
            return false;
        }

        if (DateTime.TryParse(expiryString, out DateTime expiry) && DateTime.Now > expiry)
        {
            // 验证码已过期，清除Session
            ClearVerificationCode(email);
            return false;
        }

        var isValid = storedCode == inputCode;

        if (isValid)
        {
            // 验证成功后清除验证码
            ClearVerificationCode(email);
            // 设置验证通过标记，有效期10分钟
            session.SetString($"VerificationPassed_{email}", DateTime.Now.AddMinutes(10).ToString("yyyy-MM-dd HH:mm:ss"));
        }

        return isValid;
    }

    /// <summary>
    /// 清除验证码
    /// </summary>
    public void ClearVerificationCode(string email)
    {
        var session = ct.HttpContext!.Session;
        session.Remove($"VerificationCode_{email}");
        session.Remove($"VerificationCodeExpiry_{email}");
    }


    /// <summary>
    /// 发送验证码邮件
    /// </summary>
    public void SendVerificationCodeEmail(User u, string verificationCode)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Password Reset Verification Code";
        mail.IsBodyHtml = true;

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
            <p>Your verification code is:</p>
            <h1 style='color: red'>{verificationCode}</h1
            <p>From, 🐱 Rental Management</p>
        ";

        SendEmail(mail);
    }

}
