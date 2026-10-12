using System.ComponentModel.DataAnnotations;
using BaseComponents;

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
        public string? SenderName { get; set; }

        [BaseMobile]
        public string? SenderMobile { get; set; }

        [BaseTelephone]
        public string? SenderTel { get; set; }

        public string? SenderCounty { get; set; }
        public string? SenderDistrict { get; set; }
        public string? SenderZipCode { get; set; }

        [Required(ErrorMessage = "請輸入寄件人地址")]
        [StringLength(200)]
        public string? SenderAddress { get; set; }

        // ========== 收件人 ==========
        [Required(ErrorMessage = "請輸入收件人姓名")]
        [StringLength(50, ErrorMessage = "姓名最多 50 個字元")]
        public string? ReceiverName { get; set; }

        [Required(ErrorMessage = "請輸入收件人手機")]
        [BaseMobile]
        public string? ReceiverMobile { get; set; }

        public string? ReceiverCounty { get; set; }
        public string? ReceiverDistrict { get; set; }
        public string? ReceiverZipCode { get; set; }

        [Required(ErrorMessage = "請輸入收件人地址")]
        [StringLength(200)]
        public string? ReceiverAddress { get; set; }

        // ========== 郵件資訊 ==========
        [Required(ErrorMessage = "請選擇郵件種類")]
        public string? MailType { get; set; }

        [Required(ErrorMessage = "請輸入件數")]
        [Range(1, 99, ErrorMessage = "件數須介於 1-99")]
        public int? Quantity { get; set; }

        /// <summary>重量 (公斤，小數 3 位)</summary>
        [Required(ErrorMessage = "請輸入重量")]
        [Range(typeof(decimal), "0.001", "30", ErrorMessage = "重量須介於 0.001-30 公斤")]
        public decimal? WeightKg { get; set; }

        /// <summary>保價金額 (新臺幣，整數)</summary>
        [Range(typeof(decimal), "0", "1000000", ErrorMessage = "保價金額上限為 1,000,000 元")]
        public decimal? InsuredAmount { get; set; }

        [Required(ErrorMessage = "請選擇寄件日期")]
        public DateTime? ShipDate { get; set; }

        /// <summary>指定送達期間 (選填，最長 1 個月)</summary>
        public DateTime? DeliveryStart { get; set; }

        public DateTime? DeliveryEnd { get; set; }

        /// <summary>寄件方式：Counter 臨櫃、Pickup 到府收件</summary>
        public string SendMethod { get; set; } = "Counter";

        [Required(ErrorMessage = "請選擇付款方式")]
        public string? PayType { get; set; }

        public bool NeedSignature { get; set; } = true;

        [StringLength(200, ErrorMessage = "備註最多 200 個字元")]
        public string? Remark { get; set; }

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
