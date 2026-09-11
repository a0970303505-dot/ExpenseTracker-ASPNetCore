using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace expense_tracker.Models
{
    public class ApplicationUser : IdentityUser
    {
        // 新增欄位：全名 (顯示在側邊欄)
        [Column(TypeName = "nvarchar(50)")]
        public string FullName { get; set; } = "User";

        // 新增欄位：大頭貼 (存成二進位圖片檔)
        public byte[]? ProfilePicture { get; set; }
    }
}