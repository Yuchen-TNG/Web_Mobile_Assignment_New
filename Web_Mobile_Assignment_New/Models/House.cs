using System.ComponentModel.DataAnnotations;

namespace Web_Mobile_Assignment_New.Models
{
    public class House
    {
        public int Id { get; set; }

        public string RoomType { get; set; }   // 对应数据库的 "Type"

        public decimal Price { get; set; }

        public int Rooms { get; set; }     // ✅ 改成 Rooms

        public int Bathrooms { get; set; } // ✅ 改成 Bathrooms

        public int Sqft { get; set; }

        public string Address { get; set; }

        public string? ImageUrl { get; set; } // ✅ 改成 ImageUrl

        public string? Other { get; set; }

        // 🆕 新增字段
       
        public string? RoomName { get; set; }  // 房间名称

        public string? RoomStatus { get; set; }  // 房间状态（Available / Rented / Maintenance）

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }  // 租期开始

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }    // 租期结束

        public string? Furnishing { get; set; }   // 家具配置（Fully / Semi / Unfurnished）
    }
}
