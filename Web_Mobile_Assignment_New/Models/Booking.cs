using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Mobile_Assignment_New.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        // Foreign key → House
        public int HouseId { get; set; }
        public House House { get; set; }

        // Foreign key → Tenant (using Email as PK in your DB)
        [Required]
        [MaxLength(100)]
        public string UserEmail { get; set; }
        public User User { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalPrice { get; set; }

        // ✅ New: One-to-one Payment
        public Payment? Payment { get; set; }
    }
}