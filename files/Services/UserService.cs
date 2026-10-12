using Microsoft.EntityFrameworkCore;
using BlazorUserManagement.Data;

namespace BlazorUserManagement.Services
{
    /// <summary>
    /// 使用者服務 - 處理使用者的 CRUD 操作
    /// </summary>
    public class UserService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<UserService> _logger;
        private readonly AuthService _authService;

        public UserService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<UserService> logger,
            AuthService authService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _authService = authService;
        }

        /// <summary>
        /// 取得所有使用者
        /// </summary>
        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var users = await context.Users
                    .OrderByDescending(u => u.CreatedDate)
                    .ToListAsync();

                return users.Select(MapToUserDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"取得使用者清單失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 取得單一使用者
        /// </summary>
        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"使用者ID {userId} 不存在");
                }

                return MapToUserDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"取得使用者 {userId} 失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 新增使用者
        /// </summary>
        public async Task<UserDto> CreateUserAsync(UserCreateEditRequest request)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                // 檢查使用者名稱是否已存在
                if (await context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    throw new InvalidOperationException($"使用者名稱 '{request.Username}' 已存在");
                }

                // 檢查電子郵件是否已存在
                if (await context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    throw new InvalidOperationException($"電子郵件 '{request.Email}' 已被使用");
                }

                // 驗證密碼
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    throw new InvalidOperationException("新增使用者時必須設定密碼");
                }

                var user = new User
                {
                    Username = request.Username.Trim(),
                    Email = request.Email.Trim(),
                    FullName = request.FullName?.Trim(),
                    PasswordHash = _authService.HashPassword(request.Password),
                    IsActive = request.IsActive,
                    CreatedDate = DateTime.Now
                };

                context.Users.Add(user);
                await context.SaveChangesAsync();

                _logger.LogInformation($"新增使用者 '{user.Username}' 成功 (ID: {user.UserId})");

                return MapToUserDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"新增使用者失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 編輯使用者
        /// </summary>
        public async Task<UserDto> UpdateUserAsync(int userId, UserCreateEditRequest request)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"使用者ID {userId} 不存在");
                }

                // 檢查使用者名稱是否被其他使用者使用
                if (user.Username != request.Username &&
                    await context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    throw new InvalidOperationException($"使用者名稱 '{request.Username}' 已被他人使用");
                }

                // 檢查電子郵件是否被其他使用者使用
                if (user.Email != request.Email &&
                    await context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    throw new InvalidOperationException($"電子郵件 '{request.Email}' 已被他人使用");
                }

                // 更新基本資訊
                user.Username = request.Username.Trim();
                user.Email = request.Email.Trim();
                user.FullName = request.FullName?.Trim();
                user.IsActive = request.IsActive;

                // 如果提供新密碼，則更新
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    user.PasswordHash = _authService.HashPassword(request.Password);
                }

                user.UpdatedDate = DateTime.Now;

                context.Users.Update(user);
                await context.SaveChangesAsync();

                _logger.LogInformation($"編輯使用者 '{user.Username}' (ID: {userId}) 成功");

                return MapToUserDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"編輯使用者 {userId} 失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 刪除使用者
        /// </summary>
        public async Task DeleteUserAsync(int userId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"使用者ID {userId} 不存在");
                }

                context.Users.Remove(user);
                await context.SaveChangesAsync();

                _logger.LogInformation($"刪除使用者 '{user.Username}' (ID: {userId}) 成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"刪除使用者 {userId} 失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 停用使用者 (軟刪除)
        /// </summary>
        public async Task DeactivateUserAsync(int userId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"使用者ID {userId} 不存在");
                }

                user.IsActive = false;
                user.UpdatedDate = DateTime.Now;

                context.Users.Update(user);
                await context.SaveChangesAsync();

                _logger.LogInformation($"停用使用者 '{user.Username}' (ID: {userId}) 成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"停用使用者 {userId} 失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 啟用使用者
        /// </summary>
        public async Task ActivateUserAsync(int userId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"使用者ID {userId} 不存在");
                }

                user.IsActive = true;
                user.UpdatedDate = DateTime.Now;

                context.Users.Update(user);
                await context.SaveChangesAsync();

                _logger.LogInformation($"啟用使用者 '{user.Username}' (ID: {userId}) 成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"啟用使用者 {userId} 失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 變更密碼
        /// </summary>
        public async Task ChangePasswordAsync(int userId, string newPassword)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    throw new InvalidOperationException("新密碼必須至少6個字元");
                }

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"使用者ID {userId} 不存在");
                }

                user.PasswordHash = _authService.HashPassword(newPassword);
                user.UpdatedDate = DateTime.Now;

                context.Users.Update(user);
                await context.SaveChangesAsync();

                _logger.LogInformation($"使用者 '{user.Username}' (ID: {userId}) 密碼變更成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"變更使用者 {userId} 密碼失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 搜尋使用者
        /// </summary>
        public async Task<List<UserDto>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var users = await context.Users
                    .Where(u => u.Username.Contains(searchTerm) ||
                                u.Email.Contains(searchTerm) ||
                                u.FullName.Contains(searchTerm))
                    .OrderByDescending(u => u.CreatedDate)
                    .ToListAsync();

                return users.Select(MapToUserDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"搜尋使用者失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 依條件查詢使用者 (條件為空時不篩選)
        /// </summary>
        public async Task<List<UserDto>> QueryUsersAsync(UserQuery query)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var users = context.Users.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(query.Username))
                {
                    users = users.Where(u => u.Username.Contains(query.Username.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(query.Email))
                {
                    users = users.Where(u => u.Email.Contains(query.Email.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(query.FullName))
                {
                    users = users.Where(u => u.FullName != null && u.FullName.Contains(query.FullName.Trim()));
                }

                if (query.IsActive.HasValue)
                {
                    users = users.Where(u => u.IsActive == query.IsActive.Value);
                }

                var result = await users
                    .OrderBy(u => u.UserId)
                    .ToListAsync();

                return result.Select(MapToUserDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"查詢使用者失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 將 User 轉換為 UserDto
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
