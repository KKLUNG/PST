using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorUserManagement.Data
{
    /// <summary>
    /// 參數檔 (資料表 Parameters，由 CreateCommonTables.sql 建立)
    /// </summary>
    [Table("Parameters")]
    public class Parameter
    {
        /// <summary>參數群組，例如 UNIT</summary>
        [StringLength(30)]
        public string SetItem { get; set; } = string.Empty;

        /// <summary>代碼</summary>
        [StringLength(30)]
        public string SetType { get; set; } = string.Empty;

        /// <summary>顯示名稱</summary>
        [StringLength(100)]
        public string SetValue { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        [StringLength(200)]
        public string? Memo { get; set; }

        public bool IsDeleted { get; set; }
    }

    /// <summary>
    /// 地址行政區與郵遞區號 (資料表 AddressAreas，由 CreateCommonTables.sql 建立)
    /// </summary>
    [Table("AddressAreas")]
    public class AddressArea
    {
        [StringLength(10)]
        public string County { get; set; } = string.Empty;

        [StringLength(10)]
        public string District { get; set; } = string.Empty;

        [StringLength(3)]
        public string ZipCode { get; set; } = string.Empty;

        public int SortOrder { get; set; }
    }
}
