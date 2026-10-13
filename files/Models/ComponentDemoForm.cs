using System.ComponentModel.DataAnnotations;
using BaseComponents;
using BlazorUserManagement.Data.Cms;

namespace BlazorUserManagement.Models
{
    /// <summary>
    /// 底層元件展示頁的表單 (驗證規則以 DataAnnotations 設定；儲存到 CMSData)
    /// </summary>
    public class ComponentDemoForm
    {
        /// <summary>CMSData 中的畫面名稱</summary>
        public const string CmsPageName = "ComponentGallery";

        [Required(ErrorMessage = "請輸入帳號")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "帳號長度須為 3-10 個字元")]
        [CmsColumn("SX01")]
        public string? UserId { get; set; }

        [Required(ErrorMessage = "請輸入局號")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "局號須為 6 位數字")]
        [CmsColumn("SX02")]
        public string? BranchNo { get; set; }

        [CmsColumn("SX03")]
        public string? ProductCode { get; set; }

        /// <summary>密碼僅示範輸入框，不儲存</summary>
        public string? Password { get; set; }

        [Required(ErrorMessage = "請選擇單位")]
        [CmsColumn("SX04")]
        public string? Unit { get; set; }

        [CmsColumn("SX05")]
        public bool? Status { get; set; }

        [Range(1, 9999, ErrorMessage = "數量須介於 1-9999")]
        [CmsColumn("SX06")]
        public int? Qty { get; set; }

        [Range(0, 100000000, ErrorMessage = "金額不可為負數")]
        [CmsColumn("SX07")]
        public decimal? Amount { get; set; }

        [Required(ErrorMessage = "請選擇日期")]
        [CmsColumn("DA01")]
        public DateTime? AdDate { get; set; }

        [CmsColumn("DA02")]
        public DateTime? RocDate { get; set; }

        [CmsColumn("SX08")]
        public string Gender { get; set; } = "M";

        [CmsColumn("SX09")]
        public bool Agree { get; set; }

        [Required(ErrorMessage = "請選擇付款方式")]
        [CmsColumn("SX10")]
        public string? PayType { get; set; }

        [CmsColumn("SX11")]
        public string? ParamUnit { get; set; }

        [CmsColumn("DA03")]
        public DateTime? StartDate { get; set; }

        [CmsColumn("DA04")]
        public DateTime? EndDate { get; set; }

        [BaseMobile]
        [CmsColumn("SX12")]
        public string? Mobile { get; set; }

        [BaseTelephone]
        [CmsColumn("SX13")]
        public string? Tel { get; set; }

        [CmsColumn("SX14")]
        public string? County { get; set; }

        [CmsColumn("SX15")]
        public string? District { get; set; }

        [CmsColumn("SX16")]
        public string? ZipCode { get; set; }

        [Required(ErrorMessage = "請輸入地址")]
        [CmsColumn("SX17")]
        public string? AddressDetail { get; set; }

        [CmsColumn("SX18")]
        public string? FullAddress { get; set; }

        [StringLength(200, ErrorMessage = "備註最多 200 個字元")]
        [CmsColumn("SH01")]
        public string? Remark { get; set; }
    }
}
