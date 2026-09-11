using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using expense_tracker.Models;

namespace expense_tracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context; // 新增 DbContext，為了刪除關聯資料

        
        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 ApplicationDbContext context) 
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

       

        // 個人資料頁面 (GET) - 顯示帳號資訊與刪除按鈕
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            return View(user);
        }



        // GET: EditProfile (顯示編輯表單)
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            return View(user);
        }

        // POST: EditProfile (處理更新)
        [HttpPost]
        public async Task<IActionResult> EditProfile(string fullName, IFormFile? profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            // 更新名字
            if (!string.IsNullOrEmpty(fullName))
            {
                user.FullName = fullName;
            }

            // 更新圖片 (如果有上傳)
            if (profilePicture != null && profilePicture.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await profilePicture.CopyToAsync(memoryStream);
                    user.ProfilePicture = memoryStream.ToArray();
                }
            }

            // 儲存變更
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return RedirectToAction("Profile"); // 修改成功回個人頁
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(user);
        }

        // 刪除帳號 (POST) - 包含刪除所有關聯資料
        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // A. 先刪除該用戶的所有關聯資料 (交易、類別、預算)
                var transactions = _context.Transactions.Where(t => t.UserId == user.Id);
                _context.Transactions.RemoveRange(transactions);

                var categories = _context.Categories.Where(c => c.UserId == user.Id);
                _context.Categories.RemoveRange(categories);

                var budgets = _context.Budgets.Where(b => b.UserId == user.Id);
                _context.Budgets.RemoveRange(budgets);

                await _context.SaveChangesAsync();

                // B. 登出並刪除使用者
                await _signInManager.SignOutAsync();
                await _userManager.DeleteAsync(user);
            }
            // 刪除後跳轉回登入頁
            return RedirectToAction("Login");
        }


        // 1. 註冊頁面 (GET)
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Dashboard");
            return View();
        }

        // 2. 處理註冊 (POST)
        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password, IFormFile? profilePicture)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName };

                if (profilePicture != null && profilePicture.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await profilePicture.CopyToAsync(memoryStream);
                        user.ProfilePicture = memoryStream.ToArray();
                    }
                }

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View();
        }

        // 3. 登入頁面 (GET)
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Dashboard");
            return View();
        }

        // 4. 處理登入 (POST)
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(email, password, false, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Dashboard");
                }
                ModelState.AddModelError("", "登入失敗：帳號或密碼錯誤");
            }
            return View();
        }

        // 5. 登出 (POST)
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}