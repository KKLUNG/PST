using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BlazorUserManagement.Data;
using BlazorUserManagement.Services;

namespace BlazorUserManagement.Pages.Account
{
    /// <summary>
    /// 登入頁 - 驗證帳號密碼後寫入登入 Cookie
    /// </summary>
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;

        public LoginModel(AuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public LoginRequest Input { get; set; } = new();

        /// <summary>
        /// 登入後要返回的頁面 (由未登入時被導向登入頁的網址帶入)
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            // 已登入則直接進入系統
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var response = await _authService.LoginAsync(Input);
            if (!response.Success || response.User == null)
            {
                ErrorMessage = response.Message;
                return Page();
            }

            // 建立登入身分並寫入 Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, response.User.UserId.ToString()),
                new Claim(ClaimTypes.Name, response.User.Username),
                new Claim("FullName", response.User.FullName ?? "")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            // 只允許導回本站網址，避免開放式重新導向
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }
    }
}
