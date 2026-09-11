using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using expense_tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;


namespace expense_tracker.Controllers
{
    [Authorize] // 強制登入
    public class WalletController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WalletController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. 錢包列表
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .ToListAsync();
            return View(accounts);
        }

       
        // GET: Wallet/AddOrEdit
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            //準備預設圖示清單傳給 View 
            ViewBag.Icons = new List<string>
    {
        "💰", "🏦", "💳", "💵", "🏔️", "👛", "💼", "📈", "📉", "🏠",
        "🚗", "🔴", "🟡", "🟢", "🎁", "🛒", "🎓", "💠", "🎉", "💸"
    };

            if (id == 0)
            {
                return View(new expense_tracker.Models.Account());
            }
            else
            {
                var account = await _context.Accounts.FindAsync(id);
                if (account == null)
                {
                    return NotFound();
                }
                return View(account);
            }
        }

        // 新增或修改動作 (Post)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("AccountId,Title,Icon,Balance")] Account account)
        {
            var userId = _userManager.GetUserId(User);
            account.UserId = userId; // 自動綁定使用者

            
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                if (account.AccountId == 0)
                    _context.Add(account);
                else
                    _context.Update(account);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(account);
        }

        // 查看明細
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == id && a.UserId == userId);

            if (account == null) return NotFound();

            // 撈取相關交易
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.AccountId == id && t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            ViewBag.Transactions = transactions;
            return View(account);
        }

        // 刪除
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var account = await _context.Accounts.FindAsync(id);

            if (account != null && account.UserId == userId)
            {
                // 刪除關聯資料 (因為資料庫設了 Restrict)
                var transfers = _context.Transfers.Where(t => t.SourceAccountId == id || t.TargetAccountId == id);
                _context.Transfers.RemoveRange(transfers);

                var transactions = _context.Transactions.Where(t => t.AccountId == id);
                _context.Transactions.RemoveRange(transactions);

                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}