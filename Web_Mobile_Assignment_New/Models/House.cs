namespace Web_Mobile_Assignment_New.Models
{
    public class House
    {
        public int Id { get; set; } // 主键
        public string Title { get; set; } = "";
        public string Address { get; set; } = "";
        public decimal Price { get; set; }
        public string Description { get; set; } = "";
    }

}
