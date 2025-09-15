using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Mobile_Assignment_New.Models
{
    public class HouseReview
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HouseId { get; set; } // 外键到 House

        [Required, StringLength(100)]
        public string? UserEmail { get; set; } = ""; // 外键到 User.Email


        [Range(1, 5)]
        public int? Rating { get; set; } // 1-5 星、

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔹 导航属性
        public House? House { get; set; }
        public User? User { get; set; }
    }
}
