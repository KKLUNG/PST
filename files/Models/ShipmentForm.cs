using System.ComponentModel.DataAnnotations;
using BaseComponents;
using BlazorUserManagement.Data.Cms;

namespace BlazorUserManagement.Models
{
    /// <summary>
    /// 寄件登記表單 (範例畫面使用)
    /// 欄位驗證以 DataAnnotations 設定；跨欄位規則寫在 Validate()
    /// </summary>
    public class ShipmentForm : IValidatableObject
    {
        // ========== 寄件人 ==========
        [Required(ErrorMessage = "請輸入寄件人姓名")]
        [StringLength(50, ErrorMessage = "姓名最多 50 個字元")]
        [CmsColumn("SX01")]
        public string? SenderName { get; set; }

        [BaseMobile]
        [CmsColumn("SX02")]
        public string? SenderMobile { get; set; }

        [BaseTelephone]
        [CmsColumn("SX03")]
        public string? SenderTel { get; set; }

        [CmsColumn("SX04")]

        public string? SenderCounty { get; set; }
        [CmsColumn("SX05")]
        public string? SenderDistrict { get; set; }
        [CmsColumn("SX06")]
        public string? SenderZipCode { get; set; }

        [Required(ErrorMessage = "請輸入寄件人地址")]
        [StringLength(200)]
        [CmsColumn("SX07")]
        public string? SenderAddress { get; set; }

        // ========== 收件人 ==========
        [Required(ErrorMessage = "請輸入收件人姓名")]
        [StringLength(50, ErrorMessage = "姓名最多 50 個字元")]
        [CmsColumn("SX08")]
        public string? ReceiverName { get; set; }

        [Required(ErrorMessage = "請輸入收件人手機")]
        [BaseMobile]
        [CmsColumn("SX09")]
        public string? ReceiverMobile { get; set; }

        [CmsColumn("SX10")]

        public string? ReceiverCounty { get; set; }
        [CmsColumn("SX11")]
        public string? ReceiverDistrict { get; set; }
        [CmsColumn("SX12")]
        public string? ReceiverZipCode { get; set; }

        [Required(ErrorMessage = "請輸入收件人地址")]
        [StringLength(200)]
        [CmsColumn("SX13")]
        public string? ReceiverAddress { get; set; }

        // ========== 郵件資訊 ==========
        [Required(ErrorMessage = "請選擇郵件種類")]
        [CmsColumn("SX14")]
        public string? MailType { get; set; }

        [Required(ErrorMessage = "請輸入件數")]
        [Range(1, 99, ErrorMessage = "件數須介於 1-99")]
        [CmsColumn("SX15")]
        public int? Quantity { get; set; }

        /// <summary>重量 (公斤，小數 3 位)</summary>
        [Required(ErrorMessage = "請輸入重量")]
        [Range(typeof(decimal), "0.001", "30", ErrorMessage = "重量須介於 0.001-30 公斤")]
        [CmsColumn("SX16")]
        public decimal? WeightKg { get; set; }

        /// <summary>保價金額 (新臺幣，整數)</summary>
        [Range(typeof(decimal), "0", "1000000", ErrorMessage = "保價金額上限為 1,000,000 元")]
        [CmsColumn("SX17")]
        public decimal? InsuredAmount { get; set; }

        [Required(ErrorMessage = "請選擇寄件日期")]
        [CmsColumn("DA01")]
        public DateTime? ShipDate { get; set; }

        /// <summary>指定送達期間 (選填，最長 1 個月)</summary>
        [CmsColumn("DA02")]
        public DateTime? DeliveryStart { get; set; }

        [CmsColumn("DA03")]

        public DateTime? DeliveryEnd { get; set; }

        /// <summary>寄件方式：Counter 臨櫃、Pickup 到府收件</summary>
        [CmsColumn("SX18")]
        public string SendMethod { get; set; } = "Counter";

        [Required(ErrorMessage = "請選擇付款方式")]
        [CmsColumn("SX19")]
        public string? PayType { get; set; }

        [CmsColumn("SX20")]

        public bool NeedSignature { get; set; } = true;

        [StringLength(200, ErrorMessage = "備註最多 200 個字元")]
        [CmsColumn("SH01")]
        public string? Remark { get; set; }

        /// <summary>資料來源：畫面、匯入 (非使用者輸入)</summary>
        [CmsColumn("SX21")]
        public string Source { get; set; } = SourceScreen;

        public const string SourceScreen = "畫面";
        public const string SourceImport = "匯入";

        /// <summary>CMSData 中的畫面名稱</summary>
        public const string CmsPageName = "ShipmentRegister";

        /// <summary>
        /// 由匯入的 CSV 列建立 (郵件種類預設普通包裹，寄件人資料留空)
        /// </summary>
        public static ShipmentForm FromImport(Services.ShipmentCsvParser.Row row) => new()
        {
            ShipDate = row.ShipDate,
            ReceiverName = row.ReceiverName,
            ReceiverMobile = row.ReceiverMobile,
            ReceiverAddress = row.ReceiverAddress,
            Quantity = row.Quantity,
            WeightKg = row.WeightKg,
            MailType = "1",
            Source = SourceImport,
        };

        /// <summary>
        /// 跨欄位規則
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // 寄件人手機、電話至少填一個
            if (string.IsNullOrWhiteSpace(SenderMobile) && string.IsNullOrWhiteSpace(SenderTel))
            {
                yield return new ValidationResult("寄件人手機、電話至少填寫一項", [nameof(SenderMobile)]);
            }

            // 地址須選擇縣市與鄉鎮市區
            if (!string.IsNullOrWhiteSpace(SenderAddress) && string.IsNullOrWhiteSpace(SenderDistrict))
            {
                yield return new ValidationResult("請選擇寄件人地址的縣市與鄉鎮市區", [nameof(SenderAddress)]);
            }
            if (!string.IsNullOrWhiteSpace(ReceiverAddress) && string.IsNullOrWhiteSpace(ReceiverDistrict))
            {
                yield return new ValidationResult("請選擇收件人地址的縣市與鄉鎮市區", [nameof(ReceiverAddress)]);
            }

            // 寄件日期不可早於今天
            if (ShipDate.HasValue && ShipDate.Value.Date < DateTime.Today)
            {
                yield return new ValidationResult("寄件日期不可早於今天", [nameof(ShipDate)]);
            }

            // 指定送達期間須在寄件日期之後
            if (ShipDate.HasValue && DeliveryStart.HasValue && DeliveryStart.Value.Date < ShipDate.Value.Date)
            {
                yield return new ValidationResult("指定送達期間不可早於寄件日期", [nameof(DeliveryEnd)]);
            }

            // 到府收件須填寄件人手機 (收件人員聯絡用)
            if (SendMethod == "Pickup" && string.IsNullOrWhiteSpace(SenderMobile))
            {
                yield return new ValidationResult("到府收件須填寫寄件人手機", [nameof(SenderMobile)]);
            }
        }
    }

    /// <summary>
    /// 已登記的寄件 (清單顯示用)
    /// </summary>
    public record ShipmentRecord(
        Guid PKey,
        int No,
        DateTime ShipDate,
        string ReceiverName,
        string ReceiverMobile,
        string ReceiverAddress,
        string MailTypeText,
        int Quantity,
        decimal WeightKg,
        decimal? InsuredAmount,
        string Source);
}
