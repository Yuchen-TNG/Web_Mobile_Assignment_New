using System.ComponentModel.DataAnnotations;

namespace Web_Mobile_Assignment_New.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        // 🔗 Foreign key → Booking
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        [Required, MaxLength(50)]
        public string Method { get; set; } // e.g., CreditCard / BankTransfer / Cash

        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // ✅ New field
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";
        // Values could be: "Pending", "Paid", "Failed", "Refunded"
    }
}
