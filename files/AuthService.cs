using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using BlazorUserManagement.Data;
using BCrypt.Net;

namespace BlazorUserManagement.Services
{
    /// <summary>
    /// 驗證服務 - 處理登入、JWT Token 產生等
    /// </summary>
    public class AuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<AuthService> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 使用者登入驗證
        /// </summary>
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // 檢查使用者是否存在
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null)
                {
                    _logger.LogWarning($"登入失敗: 使用者 '{request.Username}' 不存在");
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "使用者名稱或密碼不正確"
                    };
                }

                // 檢查使用者是否啟用
                if (!user.IsActive)
                {
                    _logger.LogWarning($"登入失敗: 使用者 '{request.Username}' 已停用");
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "帳號已停用，請聯絡管理員"
                    };
                }

                // 驗證密碼
                if (!BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"登入失敗: 使用者 '{request.Username}' 密碼錯誤");
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "使用者名稱或密碼不正確"
                    };
                }

                // 更新最後登入時間
                user.LastLoginDate = DateTime.Now;
                await _context.SaveChangesAsync();

                // 產生 JWT Token
                var token = GenerateJwtToken(user);

                _logger.LogInformation($"使用者 '{request.Username}' 登入成功");

                return new LoginResponse
                {
                    Success = true,
                    Message = "登入成功",
                    Token = token,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"登入過程發生錯誤: {ex.Message}");
                return new LoginResponse
                {
                    Success = false,
                    Message = "登入過程發生錯誤"
                };
            }
        }

        /// <summary>
        /// 驗證 JWT Token
        /// </summary>
        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 從 Token 取得使用者資訊
        /// </summary>
        public ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 產生 JWT Token
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName ?? ""),
                new Claim("IsActive", user.IsActive.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:ExpirationMinutes"])),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// 對密碼進行 BCrypt 加密
        /// </summary>
        public string HashPassword(string password)
        {
            return BCrypt.HashPassword(password);
        }

        /// <summary>
        /// 驗證密碼
        /// </summary>
        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Verify(password, hash);
        }

        /// <summary>
        /// 將 User 轉換為 UserDto (不含敏感資訊)
        /// </summary>
        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                UpdatedDate = user.UpdatedDate,
                LastLoginDate = user.LastLoginDate
            };
        }
    }
}
