using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorUserManagement.Data
{
    /// <summary>
    /// 使用者實體類別
    /// </summary>
    [Table("Users")]
    public class User
    {
        /// <summary>
        /// 使用者ID (主鍵)
        /// </summary>
        [Key]
        public int UserId { get; set; }

        /// <summary>
        /// 使用者名稱 (唯一值)
        /// </summary>
        [Required(ErrorMessage = "使用者名稱不可為空")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "使用者名稱長度必須介於3-50個字元")]
        public string Username { get; set; }

        /// <summary>
        /// 電子郵件 (唯一值)
        /// </summary>
        [Required(ErrorMessage = "電子郵件不可為空")]
        [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
        [StringLength(100)]
        public string Email { get; set; }

        /// <summary>
        /// 密碼雜湊值 (已加密)
        /// </summary>
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        /// <summary>
        /// 使用者全名
        /// </summary>
        [StringLength(100)]
        public string FullName { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 建立日期
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新日期
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// 最後登入日期
        /// </summary>
        public DateTime? LastLoginDate { get; set; }
    }

    /// <summary>
    /// 使用者登入請求模型
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "使用者名稱不可為空")]
        public string Username { get; set; }

        [Required(ErrorMessage = "密碼不可為空")]
        public string Password { get; set; }
    }

    /// <summary>
    /// 使用者登入回應模型
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public UserDto User { get; set; }
    }

    /// <summary>
    /// 使用者資料傳輸物件 (不包含敏感資訊)
    /// </summary>
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    /// <summary>
    /// 使用者建立/編輯請求模型
    /// </summary>
    public class UserCreateEditRequest
    {
        [Required(ErrorMessage = "使用者名稱不可為空")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "使用者名稱長度必須介於3-50個字元")]
        public string Username { get; set; }

        [Required(ErrorMessage = "電子郵件不可為空")]
        [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
        public string Email { get; set; }

        [StringLength(100)]
        public string FullName { get; set; }

        public bool IsActive { get; set; } = true;

        // 新增使用者時必須設定密碼，編輯時可選 (如果要變更密碼)
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度必須至少6個字元")]
        public string Password { get; set; }
    }
}
