using Microsoft.AspNetCore.Authorization; // 引用權限控制
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using expense_tracker.Models;
using Microsoft.AspNetCore.Identity;      // 引用身分識別
using System.Dynamic;

namespace expense_tracker.Controllers
{
    [Authorize] // 強制登入才能看報表
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; 

        public ReportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        
        [Route("Report/Budget")]
        [Route("Report/Budget/{year:int}/{month:int}")]
        public async Task<IActionResult> Budget(int? year, int? month)
        {
            
            if (year == null || month == null)
            {
                return Redirect($"/Report/Budget/{DateTime.Now.Year}/{DateTime.Now.Month}");
            }

            // A. 取得當前登入者 ID
            var userId = _userManager.GetUserId(User);

            int _year = year.Value;
            int _month = month.Value;

            DateTime StartDate = new DateTime(_year, _month, 1);
            DateTime EndDate = StartDate.AddMonths(1).AddDays(-1);

            // B. 撈取預算 (資料隔離：只撈自己的)
            var budgetQuery = from b in _context.Budgets
                              where b.BudgetMonth == StartDate && b.UserId == userId // 加上 UserId 過濾
                              join c in _context.Categories on b.CategoryId equals c.CategoryId
                              select new
                              {
                                  CategoryName = c.TitleWithIcon,
                                  BudgetLimit = b.Amount,
                                  CategoryId = c.CategoryId
                              };

            var budgetList = await budgetQuery.ToListAsync();
            var reportData = new List<dynamic>();

            // C. 計算實際花費 (資料隔離：只算自己的交易)
            foreach (var item in budgetList)
            {
                var actualSpend = _context.Transactions
                    .Where(t => t.CategoryId == item.CategoryId
                             && t.Date >= StartDate && t.Date <= EndDate
                             && t.UserId == userId) // 加上 UserId 過濾，防止算到別人的錢
                    .Sum(t => t.Amount);

                dynamic record = new ExpandoObject();
                record.CategoryName = item.CategoryName;
                record.BudgetLimit = item.BudgetLimit;
                record.ActualAmount = actualSpend;

                reportData.Add(record);
            }

            ViewBag.BudgetReport = reportData;
            ViewBag.SelectedMonth = StartDate;

            return View();
        }
    }
}