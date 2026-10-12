namespace BaseComponents
{
    /// <summary>
    /// 按鈕快速鍵規則 (BaseButton 使用)
    /// </summary>
    public static class BaseAccessKeys
    {
        /// <summary>
        /// 依按鈕文字自動帶入的快速鍵 (與既有系統慣例一致)
        /// 文字完全相同或以此開頭者適用，例如「報表列印」→ R
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<string, string>
        {
            { "新增", "A" },
            { "查詢", "Y" },
            { "重設", "C" },
            { "返回", "X" },
            { "儲存", "Y" },
            { "列印", "R" },
            { "報表列印", "R" },
            { "清單列印", "R" },
            { "資料列印", "S" },
            { "匯出", "O" },
            { "匯入", "I" },
            { "確定", "Y" },
            { "取消", "N" },
        };

        /// <summary>
        /// 取得按鈕實際使用的快速鍵 (大寫)；null 表示不使用
        /// </summary>
        /// <param name="text">按鈕文字</param>
        /// <param name="accessKey">指定的快速鍵；null 依文字自動帶入，空字串表示不使用</param>
        public static string? Resolve(string text, string? accessKey)
        {
            if (accessKey is not null)
            {
                return accessKey.Length == 0 ? null : accessKey.ToUpperInvariant();
            }

            if (Defaults.TryGetValue(text, out var key))
            {
                return key;
            }

            // 以較長的文字優先比對，例如「資料列印」優先於「列印」
            return Defaults
                .Where(x => text.StartsWith(x.Key, StringComparison.Ordinal))
                .OrderByDescending(x => x.Key.Length)
                .Select(x => x.Value)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// 民國年日期格式 (BaseDatePicker 使用相同規則)
    /// </summary>
    public static class BaseDateFormat
    {
        private static readonly System.Globalization.CultureInfo RocCulture = CreateRocCulture();

        /// <summary>民國年文化設定：zh-TW 搭配 TaiwanCalendar，格式 yyy/MM/dd</summary>
        public static System.Globalization.CultureInfo Roc => RocCulture;

        private static readonly System.Globalization.TaiwanCalendar TaiwanCalendar = new();

        private static readonly System.Text.RegularExpressions.Regex RocPattern =
            new(@"^(\d{1,3})/(\d{1,2})/(\d{1,2})$");

        /// <summary>
        /// 民國年上限：超過此年 (西元 3000 年) 的日期視為誤把西元年輸入成民國年
        /// </summary>
        public const int MaxReasonableAdYear = 3000;

        /// <summary>2026/10/12 → 115/10/12 (民國年固定 3 位)；null 回傳空字串</summary>
        public static string ToRoc(DateTime? date) =>
            date is null ? string.Empty : $"{TaiwanCalendar.GetYear(date.Value):000}/{date.Value:MM/dd}";

        /// <summary>
        /// 解析民國年日期 (115/10/12，年 1-3 位)；格式錯誤回傳 false
        /// 4 位數年份 (例如 2026/10/12) 視為格式錯誤，避免被當成民國 2026 年
        /// </summary>
        public static bool TryParseRoc(string? text, out DateTime date)
        {
            date = default;
            var match = RocPattern.Match(text?.Trim() ?? string.Empty);
            if (!match.Success)
            {
                return false;
            }

            var year = int.Parse(match.Groups[1].Value);
            var month = int.Parse(match.Groups[2].Value);
            var day = int.Parse(match.Groups[3].Value);
            if (year < 1 || month < 1 || month > 12 || day < 1 || day > TaiwanCalendar.GetDaysInMonth(year, month))
            {
                return false;
            }

            date = TaiwanCalendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            return true;
        }

        /// <summary>
        /// 修正誤把西元年輸入到民國年欄位的情況：
        /// 輸入 2026/10/12 會被解析成民國 2026 年 (西元 3937 年)，此時改回西元 2026/10/12
        /// </summary>
        public static DateTime? FixAdYearTypedAsRoc(DateTime? date) =>
            date is { Year: >= MaxReasonableAdYear } d ? d.AddYears(-1911) : date;

        private static System.Globalization.CultureInfo CreateRocCulture()
        {
            var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.GetCultureInfo("zh-TW").Clone();
            culture.DateTimeFormat.Calendar = new System.Globalization.TaiwanCalendar();
            culture.DateTimeFormat.ShortDatePattern = "yyy/MM/dd";
            return culture;
        }
    }

    /// <summary>
    /// 起訖日期規則 (BaseDateRange 使用)
    /// </summary>
    public static class BaseDateRangeRules
    {
        /// <summary>
        /// 檢查起訖日期，回傳錯誤訊息；正確或任一日期未輸入時回傳 null
        /// </summary>
        /// <param name="start">起日</param>
        /// <param name="end">迄日</param>
        /// <param name="maxMonths">區間最長月數，0 表示不限制</param>
        public static string? GetError(DateTime? start, DateTime? end, int maxMonths)
        {
            if (start is null || end is null)
            {
                return null;
            }

            if (start.Value.Date > end.Value.Date)
            {
                return "起日不可大於迄日";
            }

            if (maxMonths > 0 && end.Value.Date > start.Value.Date.AddMonths(maxMonths))
            {
                return $"查詢區間最長為 {maxMonths} 個月，請重新設定";
            }

            return null;
        }
    }

    /// <summary>
    /// 電話號碼拆解與組合 (BaseMobile、BaseTelephone 使用)
    /// </summary>
    public static class BasePhoneFormat
    {
        /// <summary>手機 0912-345678 → (0912, 345678)</summary>
        public static (string? Prefix, string? Number) SplitMobile(string? value)
        {
            var parts = (value ?? string.Empty).Split('-', 2);
            return (NullIfEmpty(parts[0]), parts.Length > 1 ? NullIfEmpty(parts[1]) : null);
        }

        /// <summary>(0912, 345678) → 0912-345678；兩段皆空回傳 null</summary>
        public static string? JoinMobile(string? prefix, string? number) =>
            string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(number) ? null : $"{prefix}-{number}";

        /// <summary>市話 02-23456789#123 → (02, 23456789, 123)</summary>
        public static (string? Area, string? Number, string? Extension) SplitTelephone(string? value)
        {
            var mainAndExt = (value ?? string.Empty).Split('#', 2);
            var parts = mainAndExt[0].Split('-', 2);
            return (
                NullIfEmpty(parts[0]),
                parts.Length > 1 ? NullIfEmpty(parts[1]) : null,
                mainAndExt.Length > 1 ? NullIfEmpty(mainAndExt[1]) : null);
        }

        /// <summary>(02, 23456789, 123) → 02-23456789#123；全部空白回傳 null；不含分機時省略 #</summary>
        public static string? JoinTelephone(string? area, string? number, string? extension, bool includeExtension = true)
        {
            if (string.IsNullOrEmpty(area) && string.IsNullOrEmpty(number) && string.IsNullOrEmpty(extension))
            {
                return null;
            }

            var result = $"{area}-{number}";
            if (includeExtension && !string.IsNullOrEmpty(extension))
            {
                result += $"#{extension}";
            }
            return result;
        }

        private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
    }

    /// <summary>
    /// 上傳檔案檢查規則 (BaseFileUpload 使用)
    /// </summary>
    public static class BaseUploadRules
    {
        /// <summary>
        /// 檢查檔案，回傳錯誤訊息；通過回傳 null
        /// </summary>
        /// <param name="fileName">檔名</param>
        /// <param name="size">大小 (bytes)</param>
        /// <param name="accept">允許的副檔名，以逗號分隔，例如 ".xlsx,.csv"；空白表示不限制</param>
        /// <param name="maxFileSizeMB">單檔大小上限 (MB)</param>
        public static string? Check(string fileName, long size, string? accept, int maxFileSizeMB)
        {
            var allowed = ParseAccept(accept);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (allowed.Count > 0 && !allowed.Contains(extension))
            {
                return $"{fileName}：檔案格式不符，僅允許 {accept}";
            }

            if (size > MaxBytes(maxFileSizeMB))
            {
                return $"{fileName}：檔案過大 ({FormatSize(size)})，上限 {maxFileSizeMB} MB";
            }

            return null;
        }

        public static long MaxBytes(int maxFileSizeMB) => maxFileSizeMB * 1024L * 1024L;

        public static HashSet<string> ParseAccept(string? accept) =>
            (accept ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.ToLowerInvariant())
                .ToHashSet();

        public static string FormatSize(long bytes) => bytes switch
        {
            >= 1024 * 1024 => $"{bytes / 1024d / 1024d:0.0} MB",
            >= 1024 => $"{bytes / 1024d:0.0} KB",
            _ => $"{bytes} B",
        };
    }
}
