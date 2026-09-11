using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using expense_tracker.Models;
using Microsoft.AspNetCore.Authorization; // 引用權限控制
using Microsoft.AspNetCore.Identity;      // 引用身分識別


namespace expense_tracker.Controllers
{
    [Authorize] // 強制登入才能管理預算
    public class BudgetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; 

        public BudgetController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. 列表頁 (Index)
        public async Task<IActionResult> Index()
        {
            // A. 取得當前登入者 ID
            var userId = _userManager.GetUserId(User);

            // B. 只撈取「該使用者」的預算，並包含 Category 資訊以便顯示名稱
            var applicationDbContext = _context.Budgets
                .Include(b => b.Category)
                .Where(b => b.UserId == userId); // 資料隔離

            return View(await applicationDbContext.ToListAsync());
        }

        // 2. 新增頁 - GET
        public IActionResult Create()
        {
            var userId = _userManager.GetUserId(User);

            // 下拉選單隔離：只撈取屬於該使用者且是 Expense 的類別
            ViewBag.Categories = _context.Categories
                .Where(c => c.Type == "Expense" && c.UserId == userId)
                .ToList();

            return View();
        }

        // 3. 新增頁 - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BudgetId,CategoryId,Amount,BudgetMonth")] Budget budget)
        {
            var userId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                // 綁定使用者 ID
                budget.UserId = userId;
                _context.Add(budget);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // 失敗重刷頁面時，也要隔離下拉選單
            ViewBag.Categories = _context.Categories
                .Where(c => c.Type == "Expense" && c.UserId == userId)
                .ToList();
            return View(budget);
        }

        // 4. 修改頁 - GET
        public async Task<IActionResult> Edit(int? id)
        {
            var userId = _userManager.GetUserId(User);

            if (id == null) return NotFound();

            // 檢查該預算是否屬於當前使用者
            var budget = await _context.Budgets.FindAsync(id);
            if (budget == null || budget.UserId != userId)
            {
                return NotFound(); // 防止偷看別人資料
            }

            // 下拉選單隔離
            ViewBag.Categories = _context.Categories
                .Where(c => c.Type == "Expense" && c.UserId == userId)
                .ToList();

            return View(budget);
        }

        // 5. 修改頁 - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BudgetId,CategoryId,Amount,BudgetMonth")] Budget budget)
        {
            var userId = _userManager.GetUserId(User);

            if (id != budget.BudgetId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 先確認這筆資料是他的，防止惡意修改
                    var existing = await _context.Budgets.AsNoTracking().FirstOrDefaultAsync(b => b.BudgetId == id);

                    if (existing == null || existing.UserId != userId)
                    {
                        return NotFound();
                    }

                    // 確保 ID 不變
                    budget.UserId = userId;
                    _context.Update(budget);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Budgets.Any(e => e.BudgetId == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // 失敗重刷頁面
            ViewBag.Categories = _context.Categories
                .Where(c => c.Type == "Expense" && c.UserId == userId)
                .ToList();
            return View(budget);
        }
        // GET: Budget/History
        public async Task<IActionResult> History(string searchQuery)
        {
            var userId = _userManager.GetUserId(User);

            var budgets = _context.Budgets
                .Include(b => b.Category) 
                .Where(b => b.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                budgets = budgets.Where(b => b.Category.Title.Contains(searchQuery));
            }

            // 排序
            budgets = budgets.OrderByDescending(b => b.BudgetMonth);

            return View(await budgets.ToListAsync());
        }
        // 6. 刪除功能
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var budget = await _context.Budgets.FindAsync(id);

            // 刪除前檢查權限：只能刪除自己的
            if (budget != null && budget.UserId == userId)
            {
                _context.Budgets.Remove(budget);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}