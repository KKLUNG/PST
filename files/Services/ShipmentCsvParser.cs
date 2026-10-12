using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BaseComponents;

namespace BlazorUserManagement.Services
{
    /// <summary>
    /// 寄件批次匯入：解析 CSV 並逐列檢查
    /// 格式 (第一列為標題)：寄件日期(民國),收件人姓名,收件人手機,收件人地址,件數,重量(公斤)
    /// </summary>
    public static class ShipmentCsvParser
    {
        private static readonly Regex MobilePattern = new(@"^09\d{2}-\d{6}$");

        /// <summary>一列匯入資料</summary>
        public record Row(int LineNo, DateTime ShipDate, string ReceiverName, string ReceiverMobile, string ReceiverAddress, int Quantity, decimal WeightKg);

        /// <summary>
        /// 解析檔案內容；任一列有錯誤時 Errors 不為空 (此時不應匯入任何資料)
        /// </summary>
        public static (List<Row> Rows, List<string> Errors) Parse(byte[] content, DateTime today)
        {
            var rows = new List<Row>();
            var errors = new List<string>();

            var lines = Encoding.UTF8.GetString(content)
                .TrimStart('﻿')
                .Split('\n')
                .Select(line => line.TrimEnd('\r'))
                .ToList();

            // 第一列為標題，從第 2 列開始
            for (var i = 1; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var lineNo = i + 1;
                var columns = line.Split(',').Select(c => c.Trim()).ToArray();
                if (columns.Length < 6)
                {
                    errors.Add($"第 {lineNo} 列：欄位數不足，應為 6 欄");
                    continue;
                }

                var rowErrors = new List<string>();

                if (!BaseDateFormat.TryParseRoc(columns[0], out var shipDate))
                {
                    rowErrors.Add("寄件日期格式應為民國年 yyy/MM/dd");
                }
                else if (shipDate.Date < today.Date)
                {
                    rowErrors.Add("寄件日期不可早於今天");
                }

                if (string.IsNullOrWhiteSpace(columns[1]))
                {
                    rowErrors.Add("收件人姓名不可空白");
                }

                if (!MobilePattern.IsMatch(columns[2]))
                {
                    rowErrors.Add("收件人手機格式應為 09XX-XXXXXX");
                }

                if (string.IsNullOrWhiteSpace(columns[3]))
                {
                    rowErrors.Add("收件人地址不可空白");
                }

                if (!int.TryParse(columns[4], NumberStyles.None, CultureInfo.InvariantCulture, out var quantity) || quantity < 1 || quantity > 99)
                {
                    rowErrors.Add("件數須為 1-99 的整數");
                }

                if (!decimal.TryParse(columns[5], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var weight) || weight <= 0 || weight > 30)
                {
                    rowErrors.Add("重量須介於 0.001-30 公斤");
                }

                if (rowErrors.Count > 0)
                {
                    errors.Add($"第 {lineNo} 列：{string.Join("、", rowErrors)}");
                    continue;
                }

                rows.Add(new Row(lineNo, shipDate, columns[1], columns[2], columns[3], quantity, weight));
            }

            if (rows.Count == 0 && errors.Count == 0)
            {
                errors.Add("檔案沒有資料列");
            }

            return (rows, errors);
        }
    }
}
