using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using expense_tracker.Models;
using Microsoft.AspNetCore.Authorization; // 引用權限控制
using Microsoft.AspNetCore.Identity;      // 引用身分識別

namespace expense_tracker.Controllers
{
    [Authorize] // 強制登入才能管理類別
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; 

        public CategoryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Category
        public async Task<IActionResult> Index()
        {
            // A. 取得當前登入者 ID
            var userId = _userManager.GetUserId(User);

            // B. 只撈取「該使用者」的類別 (資料隔離)
            return View(await _context.Categories
                .Where(c => c.UserId == userId)
                .ToListAsync());
        }

        // GET: Category/AddOrEdit
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {    
            // Emoji 會顯示在前端供使用者點選
            ViewBag.Icons = new List<string>
            {
                "🍔", "🚗", "🏠", "💰", "🏥", "📚", "🎮", "✈️", "🎁", "🐶",
                "🛒", "💊", "🎓", "💼", "💡", "🔧", "📱", "💻", "🎉", "💸"
            };

            var userId = _userManager.GetUserId(User);

            if (id == 0)
            {
                // 新增模式
                return View(new Category());
            }
            else
            {
                // 編輯模式：檢查該類別是否屬於當前使用者
                var category = await _context.Categories.FindAsync(id);
                if (category == null || category.UserId != userId)
                {
                    return NotFound(); // 防止偷看或修改別人資料
                }
                return View(category);
            }
        }

        // POST: Category/AddOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("CategoryId,Title,Icon,Type")] Category category)
        {
            var userId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                if (category.CategoryId == 0)
                {
                    // 新增：綁定使用者 ID
                    category.UserId = userId;
                    _context.Add(category);
                }
                else
                {
                    // 修改：先確認這筆資料是他的，防止惡意修改
                    // 使用 AsNoTracking 避免與下面的 Update 衝突
                    var existing = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == category.CategoryId);

                    if (existing == null || existing.UserId != userId)
                    {
                        return NotFound();
                    }

                    // 確保 ID 不變
                    category.UserId = userId;
                    _context.Update(category);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var category = await _context.Categories.FindAsync(id);

            // 刪除前檢查權限：只能刪除自己的
            if (category != null && category.UserId == userId)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            // 如果不是他的資料，直接忽略並返回列表
            return RedirectToAction(nameof(Index));
        }
    }
}