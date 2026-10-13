using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BlazorUserManagement.Data.Cms
{
    /// <summary>
    /// 標註表單欄位要存放在 CMSData 的哪個欄位
    /// 用法：[CmsColumn("SX01")] public string? UserId { get; set; }
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CmsColumnAttribute : Attribute
    {
        public CmsColumnAttribute(string column)
        {
            Column = column;
        }

        /// <summary>CMSData 欄位名稱：SH01-SH06、SX01-SX64、DE01-DE08、DA01-DA08</summary>
        public string Column { get; }
    }

    /// <summary>
    /// 表單模型與 CMSData 的轉換
    /// - SH / SX (文字)：字串直接存；數字、是否、日期等以固定格式轉成文字 (不受系統語系影響)
    /// - DE (日期時間)、DA (日期，去除時間)：僅可對應 DateTime / DateTime? 屬性
    /// - 對應設定錯誤 (欄位名稱錯誤、重複對應、型別不符) 會在第一次使用時拋出例外
    /// </summary>
    public static class CmsMapper
    {
        /// <summary>是否 (bool) 以 Y / N 存放</summary>
        public const string TrueText = "Y";
        public const string FalseText = "N";

        /// <summary>日期時間存在文字欄位時的格式</summary>
        public const string DateTimeTextFormat = "yyyy-MM-dd HH:mm:ss";

        private static readonly Regex ColumnPattern = new(@"^(SH0[1-6]|SX(0[1-9]|[1-5]\d|6[0-4])|DE0[1-8]|DA0[1-8])$");

        private static readonly ConcurrentDictionary<Type, IReadOnlyList<Field>> Maps = new();

        /// <summary>一個欄位對應</summary>
        public sealed record Field(PropertyInfo Property, PropertyInfo Column)
        {
            public string ColumnName => Column.Name;
            public bool IsDateColumn => ColumnName.StartsWith("DA", StringComparison.Ordinal);
            public bool IsDateTimeColumn => ColumnName.StartsWith("DE", StringComparison.Ordinal);
            public bool IsTextColumn => !IsDateColumn && !IsDateTimeColumn;
        }

        /// <summary>
        /// 取得 (並檢查) 模型的欄位對應；設定錯誤時拋出 InvalidOperationException
        /// </summary>
        public static IReadOnlyList<Field> GetMap(Type modelType) => Maps.GetOrAdd(modelType, BuildMap);

        public static IReadOnlyList<Field> GetMap<TModel>() => GetMap(typeof(TModel));

        /// <summary>
        /// 將模型寫入 CMSData (只寫有標註的欄位，其他欄位保持原值)
        /// </summary>
        public static void Apply<TModel>(TModel model, CmsData entity)
        {
            ArgumentNullException.ThrowIfNull(model);
            foreach (var field in GetMap(model.GetType()))
            {
                var value = field.Property.GetValue(model);
                if (field.IsTextColumn)
                {
                    field.Column.SetValue(entity, ToText(value));
                }
                else
                {
                    var date = (DateTime?)value;
                    field.Column.SetValue(entity, field.IsDateColumn ? date?.Date : date);
                }
            }
        }

        /// <summary>
        /// 由 CMSData 建立模型
        /// </summary>
        public static TModel ToModel<TModel>(CmsData entity) where TModel : new()
        {
            var model = new TModel();
            Fill(entity, model);
            return model;
        }

        /// <summary>
        /// 將 CMSData 的內容填入既有的模型 (只填有標註的欄位)
        /// </summary>
        public static void Fill<TModel>(CmsData entity, TModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            foreach (var field in GetMap(model.GetType()))
            {
                var stored = field.Column.GetValue(entity);
                object? value;
                if (field.IsTextColumn)
                {
                    try
                    {
                        value = FromText((string?)stored, field.Property.PropertyType);
                    }
                    catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
                    {
                        throw new FormatException(
                            $"CMSData 欄位 {field.ColumnName} 的內容「{stored}」無法轉換為 {field.Property.Name} 的型別 {field.Property.PropertyType.Name}", ex);
                    }
                }
                else
                {
                    value = stored;
                }

                if (value is null && IsNonNullableValueType(field.Property.PropertyType))
                {
                    value = Activator.CreateInstance(field.Property.PropertyType);
                }

                field.Property.SetValue(model, value);
            }
        }

        /// <summary>
        /// 值轉成存放用的文字 (固定格式，不受系統語系影響)
        /// </summary>
        public static string? ToText(object? value) => value switch
        {
            null => null,
            string s => s,
            bool b => b ? TrueText : FalseText,
            DateTime d => d.ToString(DateTimeTextFormat, CultureInfo.InvariantCulture),
            Enum e => e.ToString(),
            Guid g => g.ToString("D"),
            // decimal 會保留輸入時的尾端 0 (1.250)，統一去除，相同數值存成相同文字以便查詢
            decimal m => m.ToString("0.############################", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => throw new NotSupportedException($"不支援存放型別 {value.GetType().Name}"),
        };

        /// <summary>
        /// 文字轉回指定型別；空白視為 null
        /// </summary>
        public static object? FromText(string? text, Type targetType)
        {
            var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (type == typeof(string))
            {
                return text;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (type == typeof(bool))
            {
                return text.Trim().ToUpperInvariant() switch
                {
                    TrueText or "TRUE" or "1" => true,
                    FalseText or "FALSE" or "0" => false,
                    _ => throw new FormatException($"「{text}」不是有效的是否值 (Y/N)"),
                };
            }

            if (type == typeof(DateTime))
            {
                return DateTime.ParseExact(text, DateTimeTextFormat, CultureInfo.InvariantCulture);
            }

            if (type == typeof(Guid))
            {
                return Guid.Parse(text);
            }

            if (type.IsEnum)
            {
                return Enum.Parse(type, text);
            }

            return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
        }

        private static IReadOnlyList<Field> BuildMap(Type modelType)
        {
            var fields = new List<Field>();
            var usedColumns = new Dictionary<string, string>();

            foreach (var property in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<CmsColumnAttribute>();
                if (attribute is null)
                {
                    continue;
                }

                var name = attribute.Column;
                if (!ColumnPattern.IsMatch(name))
                {
                    throw new InvalidOperationException(
                        $"{modelType.Name}.{property.Name}：CMSData 沒有欄位 {name} (可用 SH01-SH06、SX01-SX64、DE01-DE08、DA01-DA08)");
                }

                if (usedColumns.TryGetValue(name, out var other))
                {
                    throw new InvalidOperationException(
                        $"{modelType.Name}.{property.Name}：欄位 {name} 已被 {other} 使用，不可重複對應");
                }

                if (!property.CanRead || !property.CanWrite)
                {
                    throw new InvalidOperationException($"{modelType.Name}.{property.Name}：需可讀寫才能對應 CMSData");
                }

                var column = typeof(CmsData).GetProperty(name)!;
                var field = new Field(property, column);
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                if (!field.IsTextColumn && type != typeof(DateTime))
                {
                    throw new InvalidOperationException(
                        $"{modelType.Name}.{property.Name}：{name} 為日期欄位，只能對應 DateTime 屬性 (目前為 {property.PropertyType.Name})");
                }

                if (field.IsTextColumn && !IsTextSupported(type))
                {
                    throw new InvalidOperationException(
                        $"{modelType.Name}.{property.Name}：型別 {property.PropertyType.Name} 無法存放在文字欄位 {name}");
                }

                usedColumns[name] = property.Name;
                fields.Add(field);
            }

            return fields;
        }

        private static bool IsTextSupported(Type type) =>
            type == typeof(string) || type == typeof(bool) || type == typeof(DateTime) || type == typeof(Guid) || type.IsEnum
            || Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16
                or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64
                or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

        private static bool IsNonNullableValueType(Type type) =>
            type.IsValueType && Nullable.GetUnderlyingType(type) is null;
    }
}
