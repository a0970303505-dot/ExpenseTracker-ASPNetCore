using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using expense_tracker.Models;
using Microsoft.AspNetCore.Identity;
using System.Globalization;

namespace expense_tracker.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ActionResult> Index(string range = "7")
        {
            var userId = _userManager.GetUserId(User);

            // 處理日期範圍邏輯 (支援 7, 14, 30, 90 天)
            int days = 0;
            switch (range)
            {
                case "14":
                    days = 13;
                    ViewBag.Range = "Last 14 Days";
                    break;
                case "30":
                    days = 29;
                    ViewBag.Range = "Last 30 Days";
                    break;
                case "90":
                    days = 89;
                    ViewBag.Range = "Last 90 Days";
                    break;
                default: // 預設 7 天
                    days = 6;
                    ViewBag.Range = "Last 7 Days";
                    break;
            }

            DateTime StartDate = DateTime.Today.AddDays(-days);
            DateTime EndDate = DateTime.Today;

            // 撈取資料
            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .Where(y => y.UserId == userId)
                .ToListAsync();

            // 計算總金額
            var totalIncome = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(i => i.Amount);
            ViewBag.TotalIncome = totalIncome.ToString("C0");

            var totalExpense = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(i => i.Amount);
            ViewBag.TotalExpense = totalExpense.ToString("C0");

            ViewBag.Balance = (totalIncome - totalExpense).ToString("C0");

            // 圓餅圖資料
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            // 曲線圖資料 (Spline Chart)
            var culture = new CultureInfo("en-US");
            List<SplineChartData> chartDataList = new List<SplineChartData>();

            var incomeData = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new { Date = k.Key, Amount = k.Sum(l => l.Amount) })
                .ToList();

            var expenseData = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new { Date = k.Key, Amount = k.Sum(l => l.Amount) })
                .ToList();

            // 迴圈跑滿天數 
            for (int i = 0; i <= days; i++)
            {
                DateTime currDate = StartDate.AddDays(i);

                var income = incomeData.FirstOrDefault(x => x.Date == currDate)?.Amount ?? 0;
                var expense = expenseData.FirstOrDefault(x => x.Date == currDate)?.Amount ?? 0;

                chartDataList.Add(new SplineChartData()
                {
                    day = currDate.ToString("dd-MMM", culture),
                    income = income,
                    expense = expense
                });
            }

            ViewBag.SplineChartData = chartDataList;

            // 最近 5 筆交易
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .Where(i => i.UserId == userId)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day { get; set; }
        public int income { get; set; }
        public int expense { get; set; }
    }
}