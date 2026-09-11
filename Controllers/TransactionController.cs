using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using expense_tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace expense_tracker.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Transaction
        public async Task<IActionResult> Index(string searchQuery)
        {
            var userId = _userManager.GetUserId(User);

            var transactions = _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Account) 
                .Where(t => t.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                if (DateTime.TryParse(searchQuery, out DateTime searchDate))
                {
                    transactions = transactions.Where(t => t.Date.Date == searchDate.Date);
                }
                else
                {
                    transactions = transactions.Where(t => t.Category.Title.Contains(searchQuery));
                }
            }

            transactions = transactions.OrderByDescending(t => t.Date);

            return View(await transactions.ToListAsync());
        }

        // GET: Transaction/AddOrEdit
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            var userId = _userManager.GetUserId(User);

           
            PopulateCategories(userId);
            PopulateAccounts(userId);

            if (id == 0)
            {
                return View(new Transaction());
            }
            else
            {
                var transaction = await _context.Transactions.FindAsync(id);
                if (transaction == null || transaction.UserId != userId)
                {
                    return NotFound();
                }
                return View(transaction);
            }
        }

        // POST: Transaction/AddOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
       
        public async Task<IActionResult> AddOrEdit([Bind("TransactionId,CategoryId,AccountId,Amount,Note,Date")] Transaction transaction)
        {
            var userId = _userManager.GetUserId(User);
            transaction.UserId = userId;
            
            ModelState.Remove("Account");
            ModelState.Remove("Category");
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                transaction.UserId = userId; // 綁定使用者

                if (transaction.TransactionId == 0)
                {
                    _context.Add(transaction);
                    await UpdateAccountBalance(transaction.AccountId, transaction.CategoryId, transaction.Amount, isAdd: true);
                }
                else
                {
                    // === 修改模式 ===

                    // 1.先找出「舊的」交易紀錄 
                    var original = await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.TransactionId == transaction.TransactionId);
                    if (original == null || original.UserId != userId) return NotFound();

                    // 2. 還原舊餘額 (把舊的金額「反向」操作回去)
                    await UpdateAccountBalance(original.AccountId, original.CategoryId, original.Amount, isAdd: false);

                    // 3. 更新交易內容
                    _context.Update(transaction);

                    // 4. 套用新餘額 (把新的金額算進去)
                    await UpdateAccountBalance(transaction.AccountId, transaction.CategoryId, transaction.Amount, isAdd: true);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // 如果失敗，要重新撈選單資料
            PopulateCategories(userId);
            PopulateAccounts(userId);
            return View(transaction);
        }

        // POST: Transaction/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);

           
            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.TransactionId == id);

            if (transaction != null && transaction.UserId == userId)
            {
                // 1. 先處理餘額還原
                
                if (transaction.Account != null && transaction.Category != null)
                {
                    if (transaction.Category.Type == "Expense")
                    {
                        // 如果刪除的是「支出」，把錢加回去
                        transaction.Account.Balance += transaction.Amount;
                    }
                    else
                    {
                        // 如果刪除的是「收入」，把錢扣掉
                        transaction.Account.Balance -= transaction.Amount;
                    }

                    // 標記帳戶已被修改
                    _context.Update(transaction.Account);
                }

                // 2. 再刪除交易
                _context.Transactions.Remove(transaction);

                // 3. 一次存檔
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
       
        private async Task UpdateAccountBalance(int accountId, int categoryId, int amount, bool isAdd)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            var category = await _context.Categories.FindAsync(categoryId);

            if (account != null && category != null)
            {
                // 判斷是「收入」還是「支出」
                bool isExpense = category.Type == "Expense";

                // 如果是「新增交易」 (isAdd = true)
                if (isAdd)
                {
                    if (isExpense)
                        account.Balance -= amount; // 支出：餘額變少
                    else
                        account.Balance += amount; // 收入：餘額變多
                }
                // 如果是「刪除/還原交易」 (isAdd = false)
                else
                {
                    if (isExpense)
                        account.Balance += amount; // 把扣掉的錢加回來
                    else
                        account.Balance -= amount; // 把加過的錢扣回去
                }

                _context.Update(account);
            }
        }

       
        // GET: Transaction/History
        public async Task<IActionResult> History(string searchQuery, string dateRange)
        {
            var userId = _userManager.GetUserId(User);

            var query = _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Account) 
                .Where(t => t.UserId == userId)
                .AsQueryable();

            // 1. 日期篩選 
            
            if (!string.IsNullOrEmpty(dateRange) && dateRange.Length >= 20)
            {
                try
                {
                    string startStr = dateRange.Substring(0, 10);
                    string endStr = dateRange.Substring(dateRange.Length - 10, 10);

                    if (DateTime.TryParse(startStr, out DateTime s)) query = query.Where(t => t.Date >= s);
                    if (DateTime.TryParse(endStr, out DateTime e)) query = query.Where(t => t.Date <= e);
                }
                catch { /* 忽略格式錯誤 */ }
            }

            // 2. 關鍵字搜尋 
            
            bool isCategorySearch = false;
            if (!string.IsNullOrEmpty(searchQuery))
            {
                
                if (!DateTime.TryParse(searchQuery, out _))
                {
                    query = query.Where(t =>
                        (t.Note != null && t.Note.Contains(searchQuery)) ||
                        t.Category.Title.Contains(searchQuery) ||
                        t.Account.Title.Contains(searchQuery) 
                    );
                    isCategorySearch = true;
                }
            }

            // 3. 排序 (最新的在最上面)
            query = query.OrderByDescending(t => t.Date);

            var transactionList = await query.ToListAsync();

            // 4. 計算面板數字 
            var totalIncome = transactionList.Where(t => t.Category.Type == "Income").Sum(t => t.Amount);
            var totalExpense = transactionList.Where(t => t.Category.Type == "Expense").Sum(t => t.Amount);

            ViewBag.TotalIncome = totalIncome.ToString("C0");
            ViewBag.TotalExpense = totalExpense.ToString("C0");
            ViewBag.Balance = (totalIncome - totalExpense).ToString("C0");
            ViewBag.DateRange = dateRange; 

            // 5. 圖表資料 
            double totalAmount = transactionList.Sum(t => t.Amount);

            if (isCategorySearch && transactionList.Any())
            {
                ViewBag.ChartTitle = $"Trend for '{searchQuery}'";
                ViewBag.ChartData = transactionList
                    .GroupBy(t => t.Date)
                    .Select(g => new
                    {
                        xValue = g.Key.ToString("MMM dd"),
                        yValue = g.Sum(t => t.Amount),
                        text = totalAmount > 0 ? (g.Sum(t => t.Amount) / totalAmount).ToString("P0") : "0%"
                    })
                    .OrderBy(x => x.xValue)
                    .ToList();
            }
            else
            {
                ViewBag.ChartTitle = "Expenses by Category";
                ViewBag.ChartData = transactionList
                    .GroupBy(t => t.Category.TitleWithIcon)
                    .Select(g => new
                    {
                        xValue = g.Key,
                        yValue = g.Sum(t => t.Amount),
                        text = totalAmount > 0 ? (g.Sum(t => t.Amount) / totalAmount).ToString("P0") : "0%"
                    })
                    .OrderByDescending(x => x.yValue)
                    .ToList();
            }

            return View(transactionList);
        }

        [NonAction]
        public void PopulateCategories(string userId)
        {
            var CategoryCollection = _context.Categories
                .Where(c => c.UserId == userId)
                .ToList();

            Category DefaultCategory = new Category() { CategoryId = 0, Title = "Choose a Category" };
            CategoryCollection.Insert(0, DefaultCategory);
            ViewBag.Categories = CategoryCollection;
        }

       
        [NonAction]
        public void PopulateAccounts(string userId)
        {
            var accountCollection = _context.Accounts
                .Where(a => a.UserId == userId)
                .ToList();

            Account defaultAccount = new Account() { AccountId = 0, Title = "Choose an Account", Icon = "" };
            accountCollection.Insert(0, defaultAccount);

            ViewBag.Accounts = accountCollection;
        }
    }
}