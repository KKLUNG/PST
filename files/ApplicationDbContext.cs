using Microsoft.EntityFrameworkCore;

namespace BlazorUserManagement.Data
{
    /// <summary>
    /// 應用程式資料庫上下文
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// 使用者資料集
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// 設定模型約定
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 使用者實體配置
            modelBuilder.Entity<User>(entity =>
            {
                // 主鍵
                entity.HasKey(e => e.UserId);

                // 唯一索引
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                // 欄位配置
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.FullName)
                    .HasMaxLength(100);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedDate)
                    .HasDefaultValue(DateTime.Now)
                    .ValueGeneratedOnAdd();
            });

            // 預設資料 (選用)
            SeedDefaultData(modelBuilder);
        }

        /// <summary>
        /// 植入預設資料
        /// </summary>
        private void SeedDefaultData(ModelBuilder modelBuilder)
        {
            // 密碼加密範例: admin123 使用 BCrypt 加密後的結果
            // 實際應用中應在應用程式層進行加密
            var hashedPassword = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36XQuvKm";

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    Email = "admin@company.com",
                    PasswordHash = hashedPassword,
                    FullName = "系統管理員",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    LastLoginDate = DateTime.Now
                },
                new User
                {
                    UserId = 2,
                    Username = "user1",
                    Email = "user1@company.com",
                    PasswordHash = hashedPassword,
                    FullName = "李使用者",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            );
        }
    }
}
