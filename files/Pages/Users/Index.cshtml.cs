using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using BlazorUserManagement.Data;
using BlazorUserManagement.Services;

namespace BlazorUserManagement.Pages.Users
{
    /// <summary>
    /// 使用者查詢頁 - 依條件查詢，並提供編輯、刪除
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly UserService _userService;

        public IndexModel(UserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// 查詢條件 (以 GET 參數帶入，查詢結果網址可直接分享或重新整理)
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public UserQuery Query { get; set; } = new();

        public List<UserDto> Users { get; set; } = new();

        public async Task OnGetAsync()
        {
            Users = await _userService.QueryUsersAsync(Query);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // 不允許刪除目前登入的帳號
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
            {
                SetMessage("不可刪除目前登入的帳號", "error");
                return RedirectToQuery();
            }

            try
            {
                await _userService.DeleteUserAsync(id);
                SetMessage("使用者已刪除");
            }
            catch (InvalidOperationException ex)
            {
                SetMessage($"刪除失敗：{ex.Message}", "error");
            }

            return RedirectToQuery();
        }

        private void SetMessage(string message, string type = "success")
        {
            TempData["Message"] = message;
            TempData["MessageType"] = type;
        }

        /// <summary>
        /// 回到查詢頁並保留原本的查詢條件
        /// </summary>
        private IActionResult RedirectToQuery()
        {
            var routeValues = new RouteValueDictionary();
            if (!string.IsNullOrWhiteSpace(Query.Username)) routeValues["Query.Username"] = Query.Username;
            if (!string.IsNullOrWhiteSpace(Query.Email)) routeValues["Query.Email"] = Query.Email;
            if (!string.IsNullOrWhiteSpace(Query.FullName)) routeValues["Query.FullName"] = Query.FullName;
            if (Query.IsActive.HasValue) routeValues["Query.IsActive"] = Query.IsActive.Value;

            return RedirectToPage("/Users/Index", routeValues);
        }
    }
}
