using System.Globalization;
using System.Text.RegularExpressions;

namespace BaseComponents
{
    /// <summary>
    /// 數字格式化與解析規則 (BaseNumeric 使用；獨立成類別以便單元測試)
    /// </summary>
    public static class BaseNumberFormat
    {
        /// <summary>
        /// 顯示用格式：千分位 + 小數位數
        /// </summary>
        /// <param name="value">數值，null 回傳空字串</param>
        /// <param name="decimalDigits">小數位數上限</param>
        /// <param name="thousandSeparator">是否加千分位逗號</param>
        /// <param name="fixedDecimals">是否固定顯示小數位數 (例如金額 1,234.50)；否則去除尾端的 0</param>
        public static string Format(decimal? value, int decimalDigits, bool thousandSeparator = true, bool fixedDecimals = false)
        {
            if (value is null)
            {
                return string.Empty;
            }

            var integerPart = thousandSeparator ? "#,##0" : "0";
            var decimalPart = decimalDigits > 0
                ? "." + new string(fixedDecimals ? '0' : '#', decimalDigits)
                : string.Empty;

            return value.Value.ToString(integerPart + decimalPart, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 編輯用格式：不含千分位、去除尾端的 0 (例如 1234.5)
        /// </summary>
        public static string FormatRaw(decimal? value) =>
            value?.ToString("0.############################", CultureInfo.InvariantCulture) ?? string.Empty;

        /// <summary>
        /// 解析輸入文字 (可含千分位逗號與空白)；空白回傳 null
        /// </summary>
        public static bool TryParse(string? text, out decimal? value)
        {
            value = null;
            var cleaned = (text ?? string.Empty).Replace(",", string.Empty).Trim();
            if (cleaned.Length == 0)
            {
                return true;
            }

            if (decimal.TryParse(cleaned, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 輸入中的文字是否允許 (整數位數、小數位數、是否可為負數)
        /// 與 base-components.js 的輸入限制規則相同
        /// </summary>
        public static bool IsAllowedInput(string? text, int integerDigits, int decimalDigits, bool allowNegative) =>
            InputPattern(integerDigits, decimalDigits, allowNegative).IsMatch(text ?? string.Empty);

        /// <summary>
        /// 輸入限制的正規表示式 (例如整數 3 位、小數 2 位：^\d{0,3}(\.\d{0,2})?$)
        /// </summary>
        public static Regex InputPattern(int integerDigits, int decimalDigits, bool allowNegative)
        {
            var sign = allowNegative ? "-?" : string.Empty;
            var decimals = decimalDigits > 0 ? $@"(\.\d{{0,{decimalDigits}}})?" : string.Empty;
            return new Regex($@"^{sign}\d{{0,{integerDigits}}}{decimals}$");
        }

        /// <summary>
        /// 四捨五入到指定小數位數，並限制在範圍內
        /// </summary>
        public static decimal? Normalize(decimal? value, int decimalDigits, decimal? min, decimal? max)
        {
            if (value is null)
            {
                return null;
            }

            var result = Math.Round(value.Value, decimalDigits, MidpointRounding.AwayFromZero);
            if (min.HasValue && result < min.Value)
            {
                result = min.Value;
            }
            if (max.HasValue && result > max.Value)
            {
                result = max.Value;
            }
            return result;
        }

        /// <summary>
        /// 將 decimal 轉成元件綁定的數值型別 (int、long、decimal、double 及其可為 null 型別)
        /// 不可為 null 的型別遇到 null 時回傳 0
        /// </summary>
        public static TValue? ToValue<TValue>(decimal? value)
        {
            var underlying = Nullable.GetUnderlyingType(typeof(TValue));
            if (value is null)
            {
                return underlying is not null ? default : (TValue)Convert.ChangeType(0, typeof(TValue), CultureInfo.InvariantCulture);
            }

            return (TValue)Convert.ChangeType(value.Value, underlying ?? typeof(TValue), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 將綁定的數值轉成 decimal
        /// </summary>
        public static decimal? FromValue<TValue>(TValue? value) =>
            value is null ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);

        /// <summary>
        /// 是否為整數型別
        /// </summary>
        public static bool IsIntegerType<TValue>()
        {
            var type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
            return Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16
                or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64;
        }
    }
}
