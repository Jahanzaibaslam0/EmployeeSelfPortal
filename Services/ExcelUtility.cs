using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

namespace HRMS.Services
{
    public class FileDownload
    {
        public byte[] Content { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }

    public static class ExcelUtility
    {
        public const string ExcelMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public static FileDownload ToFile(XLWorkbook workbook, string filePrefix)
        {
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return new FileDownload
                {
                    Content = stream.ToArray(),
                    ContentType = ExcelMime,
                    FileName = $"{filePrefix}_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                };
            }
        }

        public static void WriteHeaders(IXLWorksheet sheet, IReadOnlyList<string> headers)
        {
            for (var col = 0; col < headers.Count; col++)
            {
                sheet.Cell(1, col + 1).Value = headers[col];
                sheet.Cell(1, col + 1).Style.Font.Bold = true;
            }
        }

        public static string CellText(IXLRow row, int column) =>
            row.Cell(column).GetString().Trim();

        public static int TryInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            return int.TryParse(value, out var n) ? n : 0;
        }

        public static decimal? TryDecimal(string value) =>
            decimal.TryParse(value, out var d) ? d : (decimal?)null;

        public static int? TryNullableInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return int.TryParse(value, out var n) ? n : (int?)null;
        }

        public static bool ReadBool(string value, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            var v = value.Trim();
            return v.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || v.Equals("true", StringComparison.OrdinalIgnoreCase)
                || v.Equals("1", StringComparison.OrdinalIgnoreCase)
                || v.Equals("active", StringComparison.OrdinalIgnoreCase)
                || v.Equals("y", StringComparison.OrdinalIgnoreCase);
        }

        public static DateTime? ReadDate(IXLRow row, int column)
        {
            var cell = row.Cell(column);
            if (cell.IsEmpty()) return null;
            if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime().Date;
            if (DateTime.TryParse(cell.GetString(), out var parsed)) return parsed.Date;
            return null;
        }

        public static string FormatDate(DateTime? value) =>
            value?.ToString("yyyy-MM-dd") ?? "";

        public static string FormatBool(bool value, bool asActive = false) =>
            asActive ? (value ? "Active" : "Inactive") : (value ? "Yes" : "No");
    }
}
