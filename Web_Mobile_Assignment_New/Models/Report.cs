using System;
using System.ComponentModel.DataAnnotations;

namespace Web_Mobile_Assignment_New.Models
{
    public class Report
    {
        public int Id { get; set; }

        [Required]
        public string? Who { get; set; } // 举报人 UserId 或用户名

        public int? TargetProperty { get; set; } // 被举报的房源 Id，可为空

        [Required]
        public string? ReportType { get; set; } // 举报类型

        public string? Details { get; set; } // 详细说明

        [EmailAddress]
        public string? TargetEmail { get; set; } // 被举报人邮箱，可为空

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending";
    }
}