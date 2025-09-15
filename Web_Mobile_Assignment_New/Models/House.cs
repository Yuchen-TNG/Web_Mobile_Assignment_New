using System.ComponentModel.DataAnnotations;

namespace Web_Mobile_Assignment_New.Models
{
    public class House
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Room Type is required.")]
        public string RoomType { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(1, double.MaxValue, ErrorMessage = "Price must be a positive number.")]
        public decimal Price { get; set; }

        [Range(1, 8, ErrorMessage = "Rooms must be between 1 and 8.")]
        public int Rooms { get; set; }

        [Range(1, 6, ErrorMessage = "Bathrooms must be between 1 and 6.")]
        public int Bathrooms { get; set; }

        [Range(700, int.MaxValue, ErrorMessage = "Sqft must be at least 700.")]
        public int Sqft { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string Address { get; set; }

        public string? ImageUrl { get; set; }

        [StringLength(500, ErrorMessage = "Other info cannot exceed 500 characters.")]
        public string? Other { get; set; }

        [Required(ErrorMessage = "Room Name is required.")]
        [StringLength(100, ErrorMessage = "Room Name cannot exceed 100 characters.")]
        public string? RoomName { get; set; }

        [Required]
        [RegularExpression("^(Available|Rented|Maintenance|Valid)$",
            ErrorMessage = "Room status must be Available, Rented, Maintenance, or Valid.")]
        public string RoomStatus { get; set; } = "Valid"; // 默认值

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Furnishing is required.")]
        [RegularExpression("^(Fully|Semi|Unfurnished)$",
            ErrorMessage = "Furnishing must be Fully, Semi or Unfurnished.")]
        public string? Furnishing { get; set; }

        // 🔗 关系表
        public ICollection<HouseReview> Reviews { get; set; } = new List<HouseReview>();
        public ICollection<HouseImage> Images { get; set; } = new List<HouseImage>();

        public string Email { get; set; } = "";
    }
}
