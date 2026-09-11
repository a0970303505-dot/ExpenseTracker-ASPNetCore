using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization; 
namespace expense_tracker.Models
{
    public class Budget
    {
        [Key]
        public int BudgetId { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int Amount { get; set; }

        public DateTime BudgetMonth { get; set; }

        public string? UserId { get; set; }

        

        [NotMapped]
        public string? CategoryTitleWithIcon
        {
            get
            {
                return Category == null ? "" : Category.Icon + " " + Category.Title;
            }
        }

        [NotMapped]
        public string? BudgetMonthGroup
        {
            get
            {
                
                return BudgetMonth.ToString("MMM yyyy", new CultureInfo("en-US"));
            }
        }
    }
}