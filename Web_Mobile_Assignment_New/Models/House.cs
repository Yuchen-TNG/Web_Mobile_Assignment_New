using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Mobile_Assignment_New.Models
{
    public class House
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RoomType { get; set; }   // 对应数据库的 "Type"

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number.")]
        public decimal Price { get; set; }

        [Range(1, 8, ErrorMessage = "Rooms must be between 1 and 8.")]
        public int Rooms { get; set; }

        [Range(1, 6, ErrorMessage = "Bathrooms must be between 1 and 6.")]
        public int Bathrooms { get; set; }

        [Range(700, int.MaxValue, ErrorMessage = "Sqft must be at least 700.")]
        public int Sqft { get; set; }

        [Required]
        public string Address { get; set; }

        public string? ImageUrl { get; set; }

        public string? Other { get; set; }

        // 🆕 新增字段
        public string? RoomName { get; set; }
        public string? RoomStatus { get; set; }  // Available / Rented / Maintenance

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public string? Furnishing { get; set; }

        public ICollection<HouseReview>? Reviews { get; set; }
        public ICollection<HouseImage>? Images { get; set; }

    }
}
