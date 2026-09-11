using Microsoft.AspNetCore.Mvc;

namespace expense_tracker.Controllers
{
    public class SearchController : Controller
    {
        // 接收 searchType，單純轉址到對應的 History 頁面
        public IActionResult GeneralSearch(string searchType)
        {
            switch (searchType)
            {
                case "Transaction":
                    return RedirectToAction("History", "Transaction"); // 跳轉到交易歷史

                case "Budget":
                    return RedirectToAction("History", "Budget"); // 跳轉到預算歷史

                default:
                    return RedirectToAction("Index", "Dashboard");
            }
        }
    }
}