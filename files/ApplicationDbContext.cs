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

                // 注意：IsActive、CreatedDate 不設定 HasDefaultValue。
                // bool 欄位若設定資料庫預設值 true，EF Core 在值為 false 時不會送出該欄位，
                // 導致「新增停用的使用者」存進資料庫後變成啟用。
                // 預設值已由 C# 屬性初始值與 CreateDatabase.sql 的 DEFAULT 處理。
            });

            // 預設資料由 CreateDatabase.sql 建立（本專案不使用 EF Migrations，HasData 不會生效）
        }
    }
}
