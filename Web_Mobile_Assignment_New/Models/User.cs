using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Web_Mobile_Assignment_New.Models;

// View Models ----------------------------------------------------------------

#nullable disable warnings

public class LoginVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }
    public bool RememberMe { get; set; }
}

public class RegisterVM
{
    [StringLength(100)]
    [EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }

    [StringLength(100)]
    public string Name { get; set; }

    public IFormFile Photo { get; set; }

    public string Category { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly Birthday { get; set; }
}

public class PendingRegisterVM
{
    public string Email { get; set; }
    public string Name { get; set; }
    public string Password { get; set; }
    public string Category { get; set; }
    public DateOnly Birthday { get; set; }

    // 临时存放上传的照片路径，而不是 IFormFile
    public string TempPhotoPath { get; set; }

}


public class UpdatePasswordVM
{
    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string Current { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string New { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("New")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }
}

public class UpdateProfileVM
{
    public string? Email { get; set; }

    [StringLength(100)]
    public string Name { get; set; }

    public string? PhotoURL { get; set; }

    public IFormFile? Photo { get; set; }

    [DataType(DataType.Date)]
    public DateOnly Birthday { get; set; }
}

public class ResetPasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

}

// 新增：验证码验证模型
public class VerifyCodeVM
{
    [Required]
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [Required(ErrorMessage = "Verification code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits.")]
    [Display(Name = "Verification Code")]
    public string VerificationCode { get; set; }

    // 倒计时剩余秒数
    public int SecondsLeft { get; set; }

    // 新增用途字段
    public string Purpose { get; set; } = "";

}

public class ReportHouseViewModel
{
    public Report Reports { get; set; }
    public House Houses { get; set; }
}






