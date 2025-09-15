using System.ComponentModel.DataAnnotations;

namespace Web_Mobile_Assignment_New.Models
{
    public class House
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RoomType { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be positive.")]
        public decimal Price { get; set; }

        [Range(1, 8)]
        public int Rooms { get; set; }

        [Range(1, 6)]
        public int Bathrooms { get; set; }

        [Range(700, int.MaxValue)]
        public int Sqft { get; set; }

        [Required]
        public string Address { get; set; }

        public string? ImageUrl { get; set; }
        public string? Other { get; set; }

        [Required(ErrorMessage = "Room Name is required.")]
        public string? RoomName { get; set; }

        public string RoomStatus { get; set; } = "Valid"; // 默认值

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public string? Furnishing { get; set; }

        public ICollection<HouseReview> Reviews { get; set; } = new List<HouseReview>();
        public ICollection<HouseImage> Images { get; set; } = new List<HouseImage>();

        // 如果不需要 Email 可删除
        public string Email { get; set; } = "";
    }
}

