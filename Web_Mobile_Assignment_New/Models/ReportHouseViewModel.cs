namespace Web_Mobile_Assignment_New.Models
{
    // 用来把 Report + House 打包给 View 使用
    public class ReportHouseViewModel2
    {
        public Report Report { get; set; }   // 举报
        public House? House { get; set; }    // 房源（可能为空）
    }
}
