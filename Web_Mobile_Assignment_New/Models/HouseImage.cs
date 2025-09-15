using System.ComponentModel.DataAnnotations;

namespace Web_Mobile_Assignment_New.Models
{
    public class HouseImage
    {
        [Key]
        public int Id { get; set; }

        public int HouseId { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        // 🔗 导航属性
        public House House { get; set; }
    }
}
