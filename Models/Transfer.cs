using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace expense_tracker.Models
{
    public class Transfer
    {
        [Key]
        public int TransferId { get; set; }

        public int SourceAccountId { get; set; }
        public Account? SourceAccount { get; set; }

        public int TargetAccountId { get; set; }
        public Account? TargetAccount { get; set; }

        public int Amount { get; set; }

        [Column(TypeName = "nvarchar(75)")]
        public string? Note { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public string? UserId { get; set; }
    }
}
