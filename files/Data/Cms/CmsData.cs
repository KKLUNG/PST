using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorUserManagement.Data.Cms
{
    /// <summary>
    /// 測試資料暫存表 (資料表 CMSData，由 CreateCommonTables.sql 建立)
    /// 各畫面共用，以 PageName 區分；畫面欄位與資料表欄位的對應以 [CmsColumn] 標註，由 CmsMapper 轉換
    /// </summary>
    [Table("CMSData")]
    public class CmsData
    {
        [Key]
        public Guid PKey { get; set; }

        /// <summary>畫面名稱 (資料屬於哪個畫面)</summary>
        [Required]
        [StringLength(256)]
        public string PageName { get; set; } = string.Empty;

        // ========== SH01-SH06：長文字 NVARCHAR(MAX) ==========
        public string? SH01 { get; set; }
        public string? SH02 { get; set; }
        public string? SH03 { get; set; }
        public string? SH04 { get; set; }
        public string? SH05 { get; set; }
        public string? SH06 { get; set; }

        // ========== SX01-SX64：一般文字 NVARCHAR(1024) ==========
        [StringLength(1024)] public string? SX01 { get; set; }
        [StringLength(1024)] public string? SX02 { get; set; }
        [StringLength(1024)] public string? SX03 { get; set; }
        [StringLength(1024)] public string? SX04 { get; set; }
        [StringLength(1024)] public string? SX05 { get; set; }
        [StringLength(1024)] public string? SX06 { get; set; }
        [StringLength(1024)] public string? SX07 { get; set; }
        [StringLength(1024)] public string? SX08 { get; set; }
        [StringLength(1024)] public string? SX09 { get; set; }
        [StringLength(1024)] public string? SX10 { get; set; }
        [StringLength(1024)] public string? SX11 { get; set; }
        [StringLength(1024)] public string? SX12 { get; set; }
        [StringLength(1024)] public string? SX13 { get; set; }
        [StringLength(1024)] public string? SX14 { get; set; }
        [StringLength(1024)] public string? SX15 { get; set; }
        [StringLength(1024)] public string? SX16 { get; set; }
        [StringLength(1024)] public string? SX17 { get; set; }
        [StringLength(1024)] public string? SX18 { get; set; }
        [StringLength(1024)] public string? SX19 { get; set; }
        [StringLength(1024)] public string? SX20 { get; set; }
        [StringLength(1024)] public string? SX21 { get; set; }
        [StringLength(1024)] public string? SX22 { get; set; }
        [StringLength(1024)] public string? SX23 { get; set; }
        [StringLength(1024)] public string? SX24 { get; set; }
        [StringLength(1024)] public string? SX25 { get; set; }
        [StringLength(1024)] public string? SX26 { get; set; }
        [StringLength(1024)] public string? SX27 { get; set; }
        [StringLength(1024)] public string? SX28 { get; set; }
        [StringLength(1024)] public string? SX29 { get; set; }
        [StringLength(1024)] public string? SX30 { get; set; }
        [StringLength(1024)] public string? SX31 { get; set; }
        [StringLength(1024)] public string? SX32 { get; set; }
        [StringLength(1024)] public string? SX33 { get; set; }
        [StringLength(1024)] public string? SX34 { get; set; }
        [StringLength(1024)] public string? SX35 { get; set; }
        [StringLength(1024)] public string? SX36 { get; set; }
        [StringLength(1024)] public string? SX37 { get; set; }
        [StringLength(1024)] public string? SX38 { get; set; }
        [StringLength(1024)] public string? SX39 { get; set; }
        [StringLength(1024)] public string? SX40 { get; set; }
        [StringLength(1024)] public string? SX41 { get; set; }
        [StringLength(1024)] public string? SX42 { get; set; }
        [StringLength(1024)] public string? SX43 { get; set; }
        [StringLength(1024)] public string? SX44 { get; set; }
        [StringLength(1024)] public string? SX45 { get; set; }
        [StringLength(1024)] public string? SX46 { get; set; }
        [StringLength(1024)] public string? SX47 { get; set; }
        [StringLength(1024)] public string? SX48 { get; set; }
        [StringLength(1024)] public string? SX49 { get; set; }
        [StringLength(1024)] public string? SX50 { get; set; }
        [StringLength(1024)] public string? SX51 { get; set; }
        [StringLength(1024)] public string? SX52 { get; set; }
        [StringLength(1024)] public string? SX53 { get; set; }
        [StringLength(1024)] public string? SX54 { get; set; }
        [StringLength(1024)] public string? SX55 { get; set; }
        [StringLength(1024)] public string? SX56 { get; set; }
        [StringLength(1024)] public string? SX57 { get; set; }
        [StringLength(1024)] public string? SX58 { get; set; }
        [StringLength(1024)] public string? SX59 { get; set; }
        [StringLength(1024)] public string? SX60 { get; set; }
        [StringLength(1024)] public string? SX61 { get; set; }
        [StringLength(1024)] public string? SX62 { get; set; }
        [StringLength(1024)] public string? SX63 { get; set; }
        [StringLength(1024)] public string? SX64 { get; set; }

        // ========== DE01-DE08：日期時間 DATETIME ==========
        [Column(TypeName = "datetime")] public DateTime? DE01 { get; set; }
        [Column(TypeName = "datetime")] public DateTime? DE02 { get; set; }
        [Column(TypeName = "datetime")] public DateTime? DE03 { get; set; }
        [Column(TypeName = "datetime")] public DateTime? DE04 { get; set; }
        [Column(TypeName = "datetime")] public DateTime? DE05 { get; set; }
        [Column(TypeName = "datetime")] public DateTime? DE06 { get; set; }
        [Column(TypeName = "datetime")] public DateTime? DE07 { get; set; }
        [Column(TypeName = "datetime")] public DateTime? DE08 { get; set; }

        // ========== DA01-DA08：日期 DATE ==========
        [Column(TypeName = "date")] public DateTime? DA01 { get; set; }
        [Column(TypeName = "date")] public DateTime? DA02 { get; set; }
        [Column(TypeName = "date")] public DateTime? DA03 { get; set; }
        [Column(TypeName = "date")] public DateTime? DA04 { get; set; }
        [Column(TypeName = "date")] public DateTime? DA05 { get; set; }
        [Column(TypeName = "date")] public DateTime? DA06 { get; set; }
        [Column(TypeName = "date")] public DateTime? DA07 { get; set; }
        [Column(TypeName = "date")] public DateTime? DA08 { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreateDate { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? ModifiedDate { get; set; }
    }
}
