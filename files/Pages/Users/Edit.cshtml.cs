using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BlazorUserManagement.Data;
using BlazorUserManagement.Services;

namespace BlazorUserManagement.Pages.Users
{
    /// <summary>
    /// 使用者新增／編輯頁 - 網址帶 id 為編輯，不帶為新增
    /// </summary>
    public class EditModel : PageModel
    {
        private readonly UserService _userService;

        public EditModel(UserService userService)
        {
            _userService = userService;
        }

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        [BindProperty]
        public UserCreateEditRequest Input { get; set; } = new();

        public bool IsEditMode => Id.HasValue;

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsEditMode)
            {
                return Page();
            }

            var user = await FindUserAsync(Id!.Value);
            if (user == null)
            {
                return UserNotFound();
            }

            Input = new UserCreateEditRequest
            {
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (IsEditMode)
            {
                // 帳號不可修改：以資料庫中的值為準，忽略表單送來的帳號
                var existing = await FindUserAsync(Id!.Value);
                if (existing == null)
                {
                    return UserNotFound();
                }
                Input.Username = existing.Username;
                ModelState.Remove("Input.Username");

                // 不可停用目前登入的帳號，避免把自己鎖在系統外
                if (!Input.IsActive && User.FindFirstValue(ClaimTypes.NameIdentifier) == Id.Value.ToString())
                {
                    ModelState.AddModelError("Input.IsActive", "不可停用目前登入的帳號");
                }
            }
            else if (string.IsNullOrWhiteSpace(Input.Password))
            {
                ModelState.AddModelError("Input.Password", "新增使用者時必須設定密碼");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                if (IsEditMode)
                {
                    await _userService.UpdateUserAsync(Id!.Value, Input);
                    TempData["Message"] = $"使用者「{Input.Username}」已更新";
                }
                else
                {
                    await _userService.CreateUserAsync(Input);
                    TempData["Message"] = $"使用者「{Input.Username}」已新增";
                }
            }
            catch (InvalidOperationException ex)
            {
                // 帳號或電子郵件重複等商業規則錯誤，顯示在表單上
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }

            TempData["MessageType"] = "success";
            return RedirectToPage("/Users/Index");
        }

        private async Task<UserDto?> FindUserAsync(int id)
        {
            try
            {
                return await _userService.GetUserByIdAsync(id);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private IActionResult UserNotFound()
        {
            TempData["Message"] = "找不到指定的使用者";
            TempData["MessageType"] = "error";
            return RedirectToPage("/Users/Index");
        }
    }
}
